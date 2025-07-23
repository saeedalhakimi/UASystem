using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.Exceptions;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Domain.DomainExceptions;

namespace UASystem.Api.Application.Services
{
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly IAppLogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(IAppLogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public OperationResult<T> HandleApplicationValidationException<T>(ApplicationModelInvalidException ex, string? correlationId)
        {
            _logger.LogError(ex, "Application validation error accoured: {errormessage}", ex.Message);
            return OperationResult<T>.Failure(new Error(ErrorCode.DomainValidationError,
                "APPLICATION_VALIDATION_ERROR", $"Application validation error occurred: {ex.Message}, {ex.Source}", correlationId));
        }

        public OperationResult<T> HandleCancelationToken<T>(OperationCanceledException ex, string? correlationId)
        {
            _logger.LogError(ex, $" {ex.Message}, {correlationId}");
            return OperationResult<T>.Failure(new Error(ErrorCode.OperationCancelled,
                $"The operation was canceled: {ex.Message}", $"{ex.GetType().Name}, {ex.Source}.", correlationId));
        }

        public OperationResult<T> HandleDomainValidationException<T>(DomainModelInvalidException ex, string? correlationId)
        {
            _logger.LogError(ex, "Domain validation error accoured: {errormessage}", ex.Message);
            return OperationResult<T>.Failure(new Error(ErrorCode.DomainValidationError,
                "DOMAIN_VALIDATION_ERROR", $"{ex.GetType().Name}, Source: {ex.Source}, Domain validation error occurred: {ex.Message}, Correlation: ", correlationId));
        }

        public OperationResult<T> HandleException<T>(Exception ex, string? correlationId)
        {
            _logger.LogError(ex, $" {ex.Message}, {correlationId}");
            return OperationResult<T>.Failure(new Error(ErrorCode.InternalServerError,
                $"An unexpected error occurred: {ex.Message}, ", $"{ex.GetType().Name}, Source: {ex.Source}.", correlationId));
        }

        public OperationResult<T> ResourceAlreadyExists<T>(string key)
        {
            throw new NotImplementedException();
        }

        public OperationResult<T> ResourceCreationFailed<T>()
        {
            throw new NotImplementedException();
        }
    }
}
