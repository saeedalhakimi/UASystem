using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.PersonDtos.Responses;
using UASystem.Api.Domain.Aggregate;

namespace UASystem.Api.Application.Contracts.PersonDtos
{
    public static class PersonMappers
    {
        public static CreatedResponseDto ToCreatedResponseDto(this Domain.Aggregate.Person person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person), "Person cannot be null.");
            }
            return new CreatedResponseDto
            {
                PersonId = person.PersonId,
                Title = person.Name.Title,
                FirstName = person.Name.FirstName,
                MiddleName = person.Name.MiddleName,
                LastName = person.Name.LastName,
                Suffix = person.Name.Suffix,
                CreatedAt = person.CreatedAt,
                CreatedBy = person.CreatedBy
            };

        }
   
        public static PersonResponseDto ToPersonResponseDto(this Domain.Aggregate.Person person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person), "Person cannot be null.");
            }
            return new PersonResponseDto
            {
                PersonId = person.PersonId,
                Title = person.Name.Title,
                FirstName = person.Name.FirstName,
                MiddleName = person.Name.MiddleName,
                LastName = person.Name.LastName,
                Suffix = person.Name.Suffix,
                CreatedAt = person.CreatedAt,
                CreatedBy = person.CreatedBy,
                UpdatedAt = person.UpdatedAt,
                UpdatedBy = person.UpdatedBy,
                IsDeleted = person.IsDeleted,
                DeletedAt = person.DeletedAt,
                DeletedBy = person.DeletedBy,
                RowVersion = person.RowVersion
            };
        }

        public static UpdatedResponseDto ToUpdatedResponseDto(byte[] oldRowVersion, byte[] newRowVersion, bool isSuccess)
        {
            return new UpdatedResponseDto
            {
                IsSuccess = isSuccess,
                OldVersion = oldRowVersion,
                NewVersion = newRowVersion
            };
        }
    }
}
