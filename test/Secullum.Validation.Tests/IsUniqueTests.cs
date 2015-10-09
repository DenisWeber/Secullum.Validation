﻿using System;
using System.Globalization;
using Xunit;

namespace Secullum.Validation.Tests
{
    public class IsUniqueTests : BaseTest, IClassFixture<PeopleDbContext>
    {
        private PeopleDbContext context;

        public IsUniqueTests(PeopleDbContext context)
        {
            this.context = context;
        }

        [Fact]
        public void IsUnique_GivenNewNameAndNewId_DontReturnErrors()
        {
            var person = new Person() { Id = 2, Name = "alex" };

            var errors = new Validation<Person>(person, context)
                .IsUnique(x => x.Name)
                .ToList();

            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void IsUnique_GivenSameNameAndSameId_DontReturnErrors()
        {
            var person = new Person() { Id = 1, Name = "fernando" };

            var errors = new Validation<Person>(person, context)
                .IsUnique(x => x.Name)
                .ToList();

            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void IsUnique_GivenNewNameAndSameId_DontReturnErrors()
        {
            var person = new Person() { Id = 1, Name = "alex" };

            var errors = new Validation<Person>(person, context)
                .IsUnique(x => x.Name)
                .ToList();

            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void IsUnique_GivenSameNameAndNewId_ReturnError()
        {
            var person = new Person() { Id = 2, Name = "fernando" };

            var errors = new Validation<Person>(person, context)
                .IsUnique(x => x.Name)
                .ToList();

            Assert.Equal(1, errors.Count);
            Assert.Equal("Name", errors[0].Property);
        }
        
        [Fact]
        public void IsUnique_GivenInvalidExpression_ThrowsException()
        {
            var person = new Person();

            Assert.Throws<ArgumentException>("expression", () =>
            {
                var erros = new Validation<Person>(person, context)
                    .IsUnique(x => "")
                    .ToList();
            });
        }

        [Fact]
        public void IsUnique_NotGivenDbContext_ThrowsException()
        {
            var person = new Person();

            Assert.Throws<ArgumentNullException>("dbContext", () =>
            {
                var erros = new Validation<Person>(person)
                    .IsUnique(x => x.Name)
                    .ToList();
            });
        }
    }
}