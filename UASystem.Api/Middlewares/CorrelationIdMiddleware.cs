using Serilog.Context;
using UASystem.Api.Application.Clocking;
using UASystem.Api.Application.ILoggingService;

namespace UASystem.Api.Middlewares
{
    /// <summary>
    /// Middleware that manages and injects a Correlation ID into the request pipeline.
    /// Ensures consistent logging and tracing across distributed systems.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAppLogger<CorrelationIdMiddleware> _logger;
        private readonly ISystemClocking _systemClocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger used for logging request information.</param>
        /// <param name="systemClocks">Provides the current UTC time.</param>
        public CorrelationIdMiddleware(RequestDelegate next, IAppLogger<CorrelationIdMiddleware> logger, ISystemClocking systemClocks)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _systemClocks = systemClocks ?? throw new ArgumentNullException(nameof(systemClocks));
        }

        /// <summary>
        /// Middleware execution logic that injects and logs the correlation ID.
        /// </summary>
        /// <param name="context">HTTP context for the request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out _))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation("Request {Method} {Path} received at {Time}",
                    context.Request.Method, context.Request.Path, _systemClocks.UtcNow);
                await _next(context);
            }
        }
    }
}
