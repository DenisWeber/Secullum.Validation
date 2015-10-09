﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Entity;
using static Secullum.Validation.Localization;
using static Secullum.Validation.Localization.StringTypes;

namespace Secullum.Validation
{
    public class Validation<T> where T : class
    {
        private T target;
        private DbContext dbContext;
        private IDictionary<MemberInfo, string> displayTextDictionary = new Dictionary<MemberInfo, string>();
        private IList<ValidationError> errorList = new List<ValidationError>();

        public Validation(T target) : this(target, null)
        {
        }

        public Validation(T target, DbContext dbContext)
        {
            this.target = target;
            this.dbContext = dbContext;
        }

        public Validation<T> HasDisplayText(Expression<Func<T, string>> expression, string displayText)
        {
            ThrowIfNotMemberAccessExpression(expression.Body);

            var memberExpression = (MemberExpression)expression.Body;

            displayTextDictionary.Add(memberExpression.Member, displayText);

            return this;
        }

        public Validation<T> IsRequired(Expression<Func<T, string>> expression)
        {
            ThrowIfNotMemberAccessExpression(expression.Body);

            var value = expression.Compile()(target);

            if (string.IsNullOrWhiteSpace(value))
            {
                AddError((MemberExpression)expression.Body, GetString(IsRequiredMessage));
            }

            return this;
        }

        public Validation<T> HasMaxLength(Expression<Func<T, string>> expression, int maxLength)
        {
            ThrowIfNotMemberAccessExpression(expression.Body);

            if (maxLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            var value = expression.Compile()(target);

            if (value != null && value.Length > maxLength)
            {
                AddError((MemberExpression)expression.Body, GetString(HasMaxLengthMessage), maxLength);
            }
            
            return this;
        }

        public Validation<T> IsEmail(Expression<Func<T, string>> expression)
        {
            ThrowIfNotMemberAccessExpression(expression.Body);

            var value = expression.Compile()(target);
            var regex = new Regex(@"^[a-zA-Z0-9_\.-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-\.]+$");
            
            if (!string.IsNullOrEmpty(value) && !regex.IsMatch(value))
            {
                AddError((MemberExpression)expression.Body, GetString(IsEmailMessage));
            }

            return this;
        }
        
        public Validation<T> IsUnique(Expression<Func<T, string>> expression)
        {
            ThrowIfNotMemberAccessExpression(expression.Body);
            
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            var idProp = typeof(T).GetTypeInfo().GetDeclaredProperty("Id");
            var idValue = idProp.GetValue(target);
            var propValue = expression.Compile()(target);

            // x.Name == "fernando"
            var equalExpression = Expression.Equal(
                expression.Body,
                Expression.Constant(propValue)
            );

            // x.Id != 1
            var idNotEqualExpression = Expression.NotEqual(
                Expression.MakeMemberAccess(expression.Parameters[0], idProp),
                Expression.Constant(idValue)
            );

            // x.Name == "fernando" && x.Id != 1
            var andExpression = Expression.AndAlso(
                equalExpression,
                idNotEqualExpression
            );

            var lambda = Expression.Lambda<Func<T, bool>>(andExpression, expression.Parameters);

            if (dbContext.Set<T>().Any(lambda))
            {
                AddError((MemberExpression)expression.Body, GetString(IsUniqueMessage));
            }
            
            return this;
        }

        public IList<ValidationError> ToList()
        {
            return new ReadOnlyCollection<ValidationError>(errorList);
        }

        private void AddError(MemberExpression expression, string message, params object[] formatArgs)
        {
            var propertyName = expression.Member.Name;
            var propertyDisplayText = "";
            var formatArgsList = new List<object>();

            displayTextDictionary.TryGetValue(expression.Member, out propertyDisplayText);
            
            formatArgsList.Add(propertyDisplayText ?? propertyName);
            formatArgsList.AddRange(formatArgs);
            
            errorList.Add(new ValidationError(propertyName, string.Format(message, formatArgsList.ToArray())));
        }

        private void ThrowIfNotMemberAccessExpression(Expression expression)
        {
            if (expression.NodeType != ExpressionType.MemberAccess)
            {
                throw new ArgumentException(GetString(InvalidExpressionMessage), nameof(expression));
            }
        }
    }
}
