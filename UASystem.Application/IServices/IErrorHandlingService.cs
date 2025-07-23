using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Exceptions;
using UASystem.Api.Application.Models;
using UASystem.Api.Domain.DomainExceptions;

namespace UASystem.Api.Application.IServices
{
    public interface IErrorHandlingService
    {
        OperationResult<T> ResourceAlreadyExists<T>(string key);
        OperationResult<T> ResourceCreationFailed<T>();
        OperationResult<T> HandleApplicationValidationException<T>(ApplicationModelInvalidException ex, string? correlationId);
        OperationResult<T> HandleDomainValidationException<T>(DomainModelInvalidException ex, string? correlationId);
        OperationResult<T> HandleException<T>(Exception ex, string correlationId);
        OperationResult<T> HandleCancelationToken<T>(OperationCanceledException ex, string correlationId);
    }
}
