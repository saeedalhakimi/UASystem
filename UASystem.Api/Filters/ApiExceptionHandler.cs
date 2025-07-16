using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using UASystem.Api.Application.ILoggingService;

namespace UASystem.Api.Filters
{
    /// <summary>
    /// MVC exception filter that handles controller-level exceptions and returns standardized responses.
    /// </summary>
    public class ApiExceptionHandler : ExceptionFilterAttribute
    {
        private readonly IAppLogger<ApiExceptionHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiExceptionHandler"/> class.
        /// </summary>
        /// <param name="logger">Logger used to log exceptions.</param>
        public ApiExceptionHandler(IAppLogger<ApiExceptionHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called when an exception occurs in a controller action.
        /// Logs the error and sets a standardized JSON result response.
        /// </summary>
        /// <param name="context">The context of the current exception.</param>
        public override void OnException(ExceptionContext context)
        {
            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });

            _logger.LogError(context.Exception, "An exception occurred in controller action. CorrelationId: {CorrelationId}", correlationId);

            var errorResponse = new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An error occurred while processing the request.",
                CorrelationId = correlationId,
                Details = context.Exception.Message // In production, limit details for security
            };

            switch (context.Exception)
            {
                case ArgumentNullException argEx:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid input provided.",
                        CorrelationId = correlationId,
                        Details = argEx.Message
                    };
                    break;

                case KeyNotFoundException keyEx:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = new
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Resource not found.",
                        CorrelationId = correlationId,
                        Details = keyEx.Message
                    };
                    break;

                // Add more specific exception types as needed
                default:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            context.Result = new JsonResult(errorResponse);
            context.ExceptionHandled = true;
        }
    }
}
