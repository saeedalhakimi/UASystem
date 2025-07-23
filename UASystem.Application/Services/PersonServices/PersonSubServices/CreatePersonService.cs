using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Application.Services.PersonServices.PersonSubServices
{
    public class CreatePersonService : ICreatePersonService
    {
        
        public Person CreatePerson(CreatePersonCommand command)
        {
            try
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
            catch (Exception)
            {

                throw;
            }
            
        }

    }
}
