using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UASystem.Api.Application.Clocking;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Routes;

namespace UASystem.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.BaseRoute)]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IAppLogger<TestController> _logger;
        private readonly ISystemClocking _systemClock;
        public TestController(IAppLogger<TestController> logger, ISystemClocking systemClock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        [HttpGet]
        public IActionResult Get()
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });

            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);

            _logger.LogInformation("Received request for test endpoint. Correlation ID: {CorrelationId}", correlationId);
            _logger.LogInformation("Request processing started at UTC time: {Time}", _systemClock.UtcNow);

            _logger.LogInformation("Test endpoint executed successfully. Correlation ID: {CorrelationId}", correlationId);

            return Ok("Hello, this is a test endpoint!");

        }
    }
}
