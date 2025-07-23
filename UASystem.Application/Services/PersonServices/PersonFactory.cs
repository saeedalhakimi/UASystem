using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Common.Utility;
using UASystem.Api.Application.Contracts.PersonDtos.Requests;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Application.Services.PersonServices
{
    public static class PersonFactory
    {
        public static CreatePersonCommand CreatePersonCommandFromDto(CreatePersonDto dto, Guid createdBy, string correlationId)
        {
            return new CreatePersonCommand
            {
                Title = StringNormalizer.Normalize(dto.Title),
                MiddleName = StringNormalizer.Normalize(dto.MiddleName),
                Suffix = StringNormalizer.Normalize(dto.Suffix),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedBy = createdBy,
                CorrelationId = correlationId
            };
        }

        public static Person CreatePerson(CreatePersonCommand command)
        {
            var personName = PersonName.Create(
                       command.FirstName,
                       command.MiddleName,
                       command.LastName,
                       command.Title,
                       command.Suffix
                   );

            var person = Person.Create(command.CreatedBy, personName);

            return person;
        }
    }
}
