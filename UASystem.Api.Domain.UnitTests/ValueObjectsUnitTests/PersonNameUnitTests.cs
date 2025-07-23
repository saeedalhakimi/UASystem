using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Domain.UnitTests.ValueObjectsTests
{
    public class PersonNameUnitTests
    {
        private const int MaxLength = 50;

        [Fact]
        public void Create_ShouldReturnPersonName_WhenValidInputsProvided()
        {
            // Arrange
            var firstName = "John";
            var middleName = "T.";
            var lastName = "Doe";
            var title = "Mr.";
            var suffix = "Jr.";

            // Act
            var result = PersonName.Create(firstName, middleName, lastName, title, suffix);

            // Assert
            Assert.Equal("John", result.FirstName);
            Assert.Equal("T.", result.MiddleName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("Mr.", result.Title);
            Assert.Equal("Jr.", result.Suffix);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_ShouldThrow_WhenFirstNameIsNullOrEmpty(string firstName)
        {
            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() =>
                PersonName.Create(firstName, "Middle", "Doe", "Mr.", "Jr."));

            Assert.Equal("First name is required.", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_ShouldThrow_WhenLastNameIsNullOrEmpty(string lastName)
        {
            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() =>
                PersonName.Create("John", "Middle", lastName, "Mr.", "Jr."));

            Assert.Equal("Last name is required.", ex.Message);
        }

        [Theory]
        [InlineData("F", 51, "First name cannot exceed")]
        [InlineData("M", 51, "Middle name cannot exceed")]
        [InlineData("L", 51, "Last name cannot exceed")]
        [InlineData("T", 51, "Title cannot exceed")]
        [InlineData("S", 51, "Suffix cannot exceed")]
        public void Create_ShouldThrow_WhenAnyFieldExceedsMaxLength(string prefix, int length, string expectedMessagePart)
        {
            var longValue = new string(prefix[0], length);

            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() =>
                PersonName.Create(
                    prefix == "F" ? longValue : "John",
                    prefix == "M" ? longValue : "Middle",
                    prefix == "L" ? longValue : "Doe",
                    prefix == "T" ? longValue : "Mr.",
                    prefix == "S" ? longValue : "Jr."
                )
            );

            Assert.Contains(expectedMessagePart, ex.Message);
        }

        [Fact]
        public void Create_ShouldTrimAllFields()
        {
            // Arrange
            var firstName = "  John ";
            var middleName = " T. ";
            var lastName = " Doe ";
            var title = " Mr. ";
            var suffix = " Jr. ";

            // Act
            var result = PersonName.Create(firstName, middleName, lastName, title, suffix);

            // Assert
            Assert.Equal("John", result.FirstName);
            Assert.Equal("T.", result.MiddleName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("Mr.", result.Title);
            Assert.Equal("Jr.", result.Suffix);
        }
    }


}
