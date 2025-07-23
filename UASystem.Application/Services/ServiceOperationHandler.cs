using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UASystem.Api.Application.Exceptions;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Domain.DomainExceptions;

namespace UASystem.Api.Application
{
    public static class ServiceOperationHandler
    {
        public static async Task<OperationResult<T>> ExecuteAsync<T, TLogger>(
            Func<Task<T>> operation,
            IAppLogger<TLogger> logger,
            IErrorHandlingService errorHandlingService,
            string operationName,
            string logMessage,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            correlationId ??= Guid.NewGuid().ToString();
            using var scope = logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            logger.LogInformation("Starting {OperationName}. {LogMessage}, CorrelationId: {CorrelationId}", operationName, logMessage, correlationId);

            try
            {
                var result = await operation();
                logger.LogInformation("Successfully executed {OperationName}. CorrelationId: {CorrelationId}", operationName, correlationId);
                return OperationResult<T>.Success(result);
            }
            catch (OperationCanceledException ex)
            {
                return errorHandlingService.HandleCancelationToken<T>(ex, correlationId);
            }
            catch (DomainModelInvalidException ex)
            {
                return errorHandlingService.HandleDomainValidationException<T>(ex, correlationId);
            }
            catch (Exception ex)
            {
                return errorHandlingService.HandleException<T>(ex, correlationId);
            }
        }
    }
}