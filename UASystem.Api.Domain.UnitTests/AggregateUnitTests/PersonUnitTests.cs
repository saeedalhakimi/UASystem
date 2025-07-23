using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Domain.UnitTests.AggregateUnitTests
{
    public class PersonUnitTests
    {
        public class PersonCreateTests
        {
            [Fact]
            public void Create_ShouldReturnPerson_WhenValidInputProvided()
            {
                // Arrange
                var createdBy = Guid.NewGuid();
                var name = PersonName.Create("John", "T.", "Doe", "Mr.", "Jr.");

                // Act
                var person = Person.Create(createdBy, name);

                // Assert
                Assert.Equal(name, person.Name);
                Assert.Equal(createdBy, person.CreatedBy);
                Assert.False(person.IsDeleted);
                Assert.NotEqual(Guid.Empty, person.PersonId);
                Assert.True((DateTime.UtcNow - person.CreatedAt).TotalSeconds < 2); // allow time tolerance
            }

            [Fact]
            public void Create_ShouldThrow_WhenCreatedByIsEmpty()
            {
                // Arrange
                var name = PersonName.Create("John", "T.", "Doe", "Mr.", "Jr.");

                // Act & Assert
                var ex = Assert.Throws<PersonNotValidException>(() => Person.Create(Guid.Empty, name));
                Assert.Equal("created By ID cannot be empty.", ex.Message);
            }

            [Fact]
            public void Create_ShouldThrow_WhenPersonNameIsNull()
            {
                // Arrange
                var createdBy = Guid.NewGuid();

                // Act & Assert
                var ex = Assert.Throws<PersonNotValidException>(() => Person.Create(createdBy, null));
                Assert.Equal("Person name cannot be null.", ex.Message);
            }
        }
        
        public class PersonReconstructTests
        {
            [Fact]
            public void Reconstruct_ShouldReturnPerson_WhenValidInputProvided()
            {
                // Arrange
                var personId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow.AddDays(-1);
                var rowVersion = new byte[] { 1, 2, 3 };

                // Act
                var person = Person.Reconstruct(
                    personId,
                    "John", "T.", "Doe", "Mr.", "Jr.",
                    createdAt,
                    Guid.NewGuid(), // createdBy
                    DateTime.UtcNow, Guid.NewGuid(), false, null, null, rowVersion
                );

                // Assert
                Assert.Equal(personId, person.PersonId);
                Assert.Equal("John", person.Name.FirstName);
                Assert.Equal("Doe", person.Name.LastName);
                Assert.Equal(createdAt, person.CreatedAt);
                Assert.Equal(rowVersion, person.RowVersion);
            }

            [Fact]
            public void Reconstruct_ShouldThrow_WhenPersonIdIsEmpty()
            {
                // Act & Assert
                var ex = Assert.Throws<PersonNotValidException>(() =>
                    Person.Reconstruct(
                        Guid.Empty,
                        "John", "T.", "Doe", "Mr.", "Jr.",
                        DateTime.UtcNow,
                        Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), false, null, null,
                        new byte[] { 1, 2 }
                    )
                );

                Assert.Equal("Person ID cannot be empty.", ex.Message);
            }

            [Fact]
            public void Reconstruct_ShouldSetEmptyRowVersion_WhenRowVersionIsNull()
            {
                // Arrange
                var personId = Guid.NewGuid();

                // Act
                var person = Person.Reconstruct(
                    personId,
                    "John", "T.", "Doe", "Mr.", "Jr.",
                    DateTime.UtcNow,
                    Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(),
                    false, null, null,
                    null
                );

                // Assert
                Assert.NotNull(person.RowVersion);
                Assert.Empty(person.RowVersion);
            }

        }

        [Fact]
        public void MarkAsDeleted_Should_Set_DeletedFields_When_Valid()
        {
            // Arrange
            var person = CreateSamplePerson();
            var deletedBy = Guid.NewGuid();

            // Act
            person.MarkAsDeleted(deletedBy);

            // Assert
            Assert.True(person.IsDeleted);
            Assert.Equal(deletedBy, person.DeletedBy);
            Assert.NotNull(person.DeletedAt);
        }

        [Fact]
        public void MarkAsDeleted_Should_Throw_When_AlreadyDeleted()
        {
            // Arrange
            var person = CreateSamplePerson();
            person.MarkAsDeleted(Guid.NewGuid());

            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() => person.MarkAsDeleted(Guid.NewGuid()));
            Assert.Equal("Person is already deleted.", ex.Message);
        }

        [Fact]
        public void Restore_Should_Clear_DeletedFields_When_Valid()
        {
            // Arrange
            var person = CreateSamplePerson();
            var deletedBy = Guid.NewGuid();
            var restoredBy = Guid.NewGuid();
            person.MarkAsDeleted(deletedBy);

            // Act
            person.Restore(restoredBy);

            // Assert
            Assert.False(person.IsDeleted);
            Assert.Null(person.DeletedBy);
            Assert.Null(person.DeletedAt);
            Assert.Equal(restoredBy, person.UpdatedBy);
            Assert.NotNull(person.UpdatedAt);
        }

        [Fact]
        public void Restore_Should_Throw_When_NotDeleted()
        {
            // Arrange
            var person = CreateSamplePerson();

            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() => person.Restore(Guid.NewGuid()));
            Assert.Equal("Person is not deleted to be restored.", ex.Message);
        }

        [Fact]
        public void UpdateName_Should_Set_Name_And_Metadata_When_Valid()
        {
            // Arrange
            var person = CreateSamplePerson();
            var updatedBy = Guid.NewGuid();
            var newName = PersonName.Create("Updated", null, "Name", null, null);

            // Act
            person.UpdateName(newName, updatedBy);

            // Assert
            Assert.Equal(newName, person.Name);
            Assert.Equal(updatedBy, person.UpdatedBy);
            Assert.NotNull(person.UpdatedAt);
        }

        [Fact]
        public void UpdateName_Should_Throw_When_Deleted()
        {
            // Arrange
            var person = CreateSamplePerson();
            person.MarkAsDeleted(Guid.NewGuid());
            var newName = PersonName.Create("New", null, "Name", null, null);

            // Act & Assert
            var ex = Assert.Throws<PersonNotValidException>(() => person.UpdateName(newName, Guid.NewGuid()));
            Assert.Equal("Cannot update a deleted person.", ex.Message);
        }

        [Fact]
        public void UpdateRowVersion_Should_Set_New_ByteArray()
        {
            // Arrange
            var person = CreateSamplePerson();
            var version = new byte[] { 1, 2, 3 };

            // Act
            person.UpdateRowVersion(version);

            // Assert
            Assert.Equal(version, person.RowVersion);
        }

        [Fact]
        public void UpdateMetadata_Should_Set_UpdatedFields()
        {
            // Arrange
            var person = CreateSamplePerson();
            var updatedBy = Guid.NewGuid();
            var updatedAt = DateTime.UtcNow;

            // Act
            person.UpdateMetadata(updatedBy, updatedAt);

            // Assert
            Assert.Equal(updatedBy, person.UpdatedBy);
            Assert.Equal(updatedAt, person.UpdatedAt);
        }


        private Person CreateSamplePerson()
        {
            var createdBy = Guid.NewGuid();
            var name = PersonName.Create("John", "M", "Doe", "Mr", "Jr");
            return Person.Create(createdBy, name);
        }
    }
}
