using System.Net;
using System.Text.Json;
using UASystem.Api.Application.ILoggingService;

namespace UASystem.Api.Middlewares
{
    /// <summary>
    /// Middleware that handles unhandled exceptions globally and returns a standard error response.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAppLogger<GlobalExceptionMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger used to log unhandled exceptions.</param>
        public GlobalExceptionMiddleware(RequestDelegate next, IAppLogger<GlobalExceptionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Middleware execution logic to catch and handle exceptions.
        /// </summary>
        /// <param name="context">HTTP context for the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles the caught exception and returns a JSON response with error details.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="exception">The caught exception to handle.</param>
        /// <returns>A task that writes the error response.</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });

            _logger.LogError(exception, "An unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred.",
                CorrelationId = correlationId,
                Details = exception.Message // In production, you might want to limit the details exposed
            };

            switch (exception)
            {
                case ArgumentNullException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid input provided.",
                        CorrelationId = correlationId,
                        Details = argEx.Message
                    };
                    break;

                case KeyNotFoundException keyEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
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
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(result);
        }
    }
}
