using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.PersonDtos.Requests;
using UASystem.Api.Application.Contracts.PersonDtos.Responses;
using UASystem.Api.Application.Models;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Application.Services.PersonServices.Queries;

namespace UASystem.Api.Application.IServices
{
    public interface IPersonService
    {
        Task<OperationResult<CreatedResponseDto>> CreatePersonAsync(CreatePersonCommand command, CancellationToken cancellationToken);
        Task<OperationResult<PersonResponseDto>> GetPersonByIdAsync(GetPersonByIdQuery request, CancellationToken cancellationToken);
        Task<OperationResult<UpdatedResponseDto>> UpdatePersonAsync(Guid personId, UpdatePersonDto request, Guid updatedBy, string? correlationId, CancellationToken cancellationToken);
    }
}
