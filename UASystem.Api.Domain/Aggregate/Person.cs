using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Domain.Aggregate
{
    public class Person
    {
        public Guid PersonId { get; private set; }
        public PersonName Name { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Guid? CreatedBy { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public Guid? UpdatedBy { get; private set; }
        public bool IsDeleted { get; private set; }
        public Guid? DeletedBy { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        private Person() { }

        public static Person Create(Guid createdBy, PersonName personName)
        {
            DomainValidator.ThrowIfEmptyGuid("createdBy", createdBy, new PersonNotValidException("created By ID cannot be empty."));
            DomainValidator.ThrowIfObjectNull("personName", personName, new PersonNotValidException("Person name cannot be null."));

            var person = new Person
            {
                PersonId = Guid.NewGuid(),
                Name = personName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                IsDeleted = false
            };

            // TODO: add domain events

            return person;
        }
        public static Person Reconstruct(Guid personId, string firstName, string? middleName,
            string lastName, string? title, string? suffix, DateTime createdAt, Guid? createdBy, DateTime? updatedAt,
            Guid? updatedBy, bool isDeleted, Guid? deletedBy, DateTime? deletedAt, byte[] rowVersion)
        {
            DomainValidator.ThrowIfEmptyGuid("personId", personId, new PersonNotValidException("Person ID cannot be empty."));

            var name = PersonName.Create(firstName, middleName, lastName, title, suffix);
            DomainValidator.ThrowIfObjectNull("name", name, new PersonNotValidException("Person name cannot be null."));

            return new Person
            {
                PersonId = personId,
                Name = name,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
                IsDeleted = isDeleted,
                DeletedBy = deletedBy,
                DeletedAt = deletedAt,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            };
        }

        public void MarkAsDeleted(Guid deletedBy)
        {
            DomainValidator.ThrowIfEmptyGuid("deletedBy", deletedBy, new PersonNotValidException("Deleted By ID cannot be empty."));
            if (IsDeleted)
            {
                throw new PersonNotValidException("Person is already deleted.");
            }
            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedAt = DateTime.UtcNow;
        }
        public void Restore(Guid restoredBy)
        {
            DomainValidator.ThrowIfEmptyGuid("restoredBy", restoredBy, new PersonNotValidException("Restored By ID cannot be empty."));
            if (!IsDeleted)
            {
                throw new PersonNotValidException("Person is not deleted to be restored.");
            }

            IsDeleted = false;
            DeletedBy = null;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = restoredBy;
        }
        public void UpdateName(PersonName newName, Guid updatedBy)
        {
            DomainValidator.ThrowIfObjectNull("newName", newName, new PersonNotValidException("New name cannot be null."));
            DomainValidator.ThrowIfEmptyGuid("updatedBy", updatedBy, new PersonNotValidException("Updated By ID cannot be empty."));
            DomainValidator.ThrowIfDeleted(IsDeleted, new PersonNotValidException("Cannot update a deleted person."));
            Name = newName;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
        public void UpdateRowVersion(byte[] newRowVersion)
        {
            DomainValidator.ThrowIfObjectNull("newRowVersion", newRowVersion, new PersonNotValidException("New row version cannot be null."));
            RowVersion = newRowVersion;
        }
        public void UpdateMetadata(Guid? updatedBy, DateTime updatedAt)
        {
            UpdatedBy = updatedBy;
            UpdatedAt = updatedAt;

        }

    }
}