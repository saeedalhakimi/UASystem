using Microsoft.AspNetCore.Mvc;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.Models;
using UASystem.Api.Models;

namespace UASystem.Api.Controllers.V1
{
    public class BaseController<T> : Controller
    {
        private readonly IAppLogger<T> _logger;

        public BaseController(IAppLogger<T> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static readonly Dictionary<ErrorCode, (int StatusCode, string StatusPhrase)> StatusMappings = new()
        {
            { ErrorCode.UnknownError, (500, "Internal Server Error") },
            { ErrorCode.ResourceNotFound, (404, "Not Found") },
            { ErrorCode.InvalidInput, (400, "Bad Request") },
            { ErrorCode.OperationCancelled, (499, "Client Closed Request") },
            { ErrorCode.InternalServerError, (500, "Internal Server Error") },
            { ErrorCode.ConflictError, (409, "Conflict") },
            { ErrorCode.Unauthorized, (401, "Unauthorized") },
            { ErrorCode.BadRequest, (400, "Bad Request") },
            { ErrorCode.DomainValidationError, (422, "Unprocessable Entity") },
            { ErrorCode.ApplicationValidationError, (422, "Unprocessable Entity") },
            { ErrorCode.Forbidden, (403, "Forbidden") },
            { ErrorCode.NotImplemented, (501, "Not Implemented") },
            { ErrorCode.ServiceUnavailable, (503, "Service Unavailable") },
            { ErrorCode.GatewayTimeout, (504, "Gateway Timeout") },
            { ErrorCode.TooManyRequests, (429, "Too Many Requests") },
            { ErrorCode.PreconditionFailed, (412, "Precondition Failed") },
            { ErrorCode.Locked, (423, "Locked") },
            { ErrorCode.NoResult, (204, "No Content") },
            { ErrorCode.InvalidData, (422, "Unprocessable Entity") },
            { ErrorCode.InvalidOperation, (400, "Bad Request") },
            { ErrorCode.DatabaseError, (500, "Internal Server Error") },
            { ErrorCode.AuthorizationError, (403, "Forbidden") },
            { ErrorCode.ResourceCreationFailed, (500, "Internal Server Error") },
            { ErrorCode.ResourceUpdateFailed, (500, "Internal Server Error") },
            { ErrorCode.ResourceDeletionFailed, (500, "Internal Server Error") },
            { ErrorCode.ResourceAlreadyExists, (409, "Conflict") },
            { ErrorCode.ValidationError, (422, "Unprocessable Entity") },
            { ErrorCode.AuthenticationError, (401, "Unauthorized") },
            { ErrorCode.PermissionDenied, (403, "Forbidden") },
            { ErrorCode.RateLimitExceeded, (429, "Too Many Requests") },
            { ErrorCode.TimeoutError, (504, "Gateway Timeout") }
        };

        protected IActionResult HandleResult<TResult>(OperationResult<TResult> result, string correlationId)
        {
            try
            {
                if (result == null)
                {
                    _logger.LogError(null, "Operation result is null. CorrelationId: {CorrelationId}", correlationId);
                    return CreateErrorResponse(
                        ErrorCode.UnknownError,
                        new List<string> { "Operation result is null." },
                        new List<string> { "The server received an invalid operation result." },
                        correlationId);
                }

                if (result.IsSuccess)
                {
                    if (result.Data == null)
                    {
                        _logger.LogWarning("Operation succeeded but returned no data. CorrelationId: {CorrelationId}", correlationId);
                        return CreateErrorResponse(
                            ErrorCode.ResourceNotFound,
                            new List<string> { "Resource not found." },
                            new List<string> { "The requested resource was not found on the server." },
                            correlationId);
                    }
                    _logger.LogInformation("Operation succeeded. CorrelationId: {CorrelationId}", correlationId);
                    return Ok(result.Data);
                }

                if (!result.Errors.Any())
                {
                    _logger.LogError(null, "No errors provided in failed operation result. CorrelationId: {CorrelationId}", correlationId);
                    return CreateErrorResponse(
                        ErrorCode.UnknownError,
                        new List<string> { "An unknown error occurred." },
                        new List<string> { "No error details provided." },
                        correlationId);
                }

                var errorMessages = result.Errors.Select(e => e.Message).ToList();
                var errorDetails = result.Errors.Select(e => e.Details).ToList();
                var errorCodes = result.Errors.Select(e => e.Code.ToString()).ToList();
                var correlationID = result.Errors.FirstOrDefault()?.CorrelationId ?? correlationId;

                _logger.LogWarning("Operation failed with errors: {Errors}. CorrelationId: {CorrelationId}",
                    string.Join(", ", errorMessages), correlationID);

                var errorCode = result.Errors.FirstOrDefault()?.Code ?? ErrorCode.UnknownError;
                var (statusCode, statusPhrase) = StatusMappings.GetValueOrDefault(errorCode, (500, "Internal Server Error"));

                var apiError = new ErrorResponse
                {
                    Timestamp = result.Timestamp != default ? result.Timestamp : DateTime.UtcNow,
                    CorrelationId = correlationID,
                    Errors = errorMessages,
                    ErrorsDetails = errorDetails,
                    ErrorCodes = errorCodes,
                    StatusCode = statusCode,
                    StatusPhrase = statusPhrase,
                    Path = HttpContext.Request.Path,
                    Method = HttpContext.Request.Method,
                    Detail = $"An error occurred while processing the request: {statusPhrase}"
                };

                return StatusCode(apiError.StatusCode, apiError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while handling operation result. CorrelationId: {CorrelationId}", correlationId);
                return CreateErrorResponse(
                    ErrorCode.InternalServerError,
                    new List<string> { "An unexpected error occurred." },
                    new List<string> { ex.Message },
                    correlationId);
            }
        }

        private IActionResult CreateErrorResponse(ErrorCode errorCode, List<string> errors, List<string> errorDetails, string? correlationId)
        {
            var (statusCode, statusPhrase) = StatusMappings.GetValueOrDefault(errorCode, (500, "Internal Server Error"));
            var apiError = new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId ?? HttpContext.TraceIdentifier,
                Errors = errors,
                ErrorsDetails = errorDetails,
                ErrorCodes = new List<string> { errorCode.ToString() },
                StatusCode = statusCode,
                StatusPhrase = statusPhrase,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Detail = $"An error occurred while processing the request: {statusPhrase}"
            };

            return StatusCode(apiError.StatusCode, apiError);
        }
    }
}