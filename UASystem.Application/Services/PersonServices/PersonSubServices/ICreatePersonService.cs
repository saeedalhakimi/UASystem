using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Domain.Aggregate;

namespace UASystem.Api.Application.Services.PersonServices.PersonSubServices
{
    public interface ICreatePersonService
    {
        Person CreatePerson(CreatePersonCommand command);
    }
}
