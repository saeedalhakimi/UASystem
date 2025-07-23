using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using UASystem.Api.Application.Clocking;
using UASystem.Api.Application.Contracts.CountryDtos;
using UASystem.Api.Application.Contracts.CountryDtos.Requests;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Filters;
using UASystem.Api.Models;
using UASystem.Api.Routes;

namespace UASystem.Api.Controllers.V1.Countries
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.BaseRoute)]
    [ApiController]
    public class CountriesController : BaseController<CountriesController>
    {
        private readonly IAppLogger<CountriesController> _logger;
        private readonly ICountryService _countryService;
        private readonly ISystemClocking _systemClock;
        public CountriesController(IAppLogger<CountriesController> logger, ICountryService countryService, ISystemClocking systemClocking) : base(logger)
        {
            _logger = logger;
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));
            _systemClock = systemClocking ?? throw new ArgumentNullException(nameof(systemClocking));
        }

        [HttpGet(Name = "GetCountries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving all countries, correlation ID: {CorrelationId}", correlationId);

            var result = await _countryService.GetCountriesAsync(correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve countries. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Successfully retrieved countries. Count: {Count}, CorrelationId: {CorrelationId}",
                result.Data.Count, correlationId);
            return Ok(result.Data);
        }

        [HttpPost(Name = "CreateCountry")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> CreateCountry([FromBody] CountryCreateDto request, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Creating country with request: {@Request}, CorrelationId: {CorrelationId}", request, correlationId);

            var result = await _countryService.CreateAsync(request, correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to create country. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }
            _logger.LogInformation("Successfully created country with ID: {CountryId}, CorrelationId: {CorrelationId}", result.Data.CountryId, correlationId);
            return CreatedAtRoute("GetById", new { countryId = result.Data.CountryId }, result);
        }

        [HttpGet(ApiRoutes.CountryRoutes.ById, Name = "GetById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById([FromRoute] int countryId, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving country by ID: {CountryId}, CorrelationId: {CorrelationId}", countryId, correlationId);

            var result = await _countryService.GetByIdAsync(countryId, correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve country. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Successfully retrieved country with ID: {CountryId}, CorrelationId: {CorrelationId}", countryId, correlationId);
            return Ok(result);
        }

        [HttpGet(ApiRoutes.CountryRoutes.ByCode, Name = "GetByCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCod([FromRoute] string countryCode, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving country by Code: {CountryCode}, CorrelationId: {CorrelationId}", countryCode, correlationId);

            var result = await _countryService.GetByCodeAsync(countryCode, correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve country. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Successfully retrieved country with code: {CountryId}, CorrelationId: {CorrelationId}", countryCode, correlationId);
            return Ok(result);
        }

        [HttpGet(ApiRoutes.CountryRoutes.ByName, Name = "GetCountryByName")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByName([FromRoute] string name, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving country by name: {countryName}, CorrelationId: {CorrelationId}", name, correlationId);

            var result = await _countryService.GetByNameAsync(name, correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve country. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Successfully retrieved country with name: {countryName}, CorrelationId: {CorrelationId}", name, correlationId);
            return Ok(result);
        }

        [HttpGet(ApiRoutes.CountryRoutes.ByCurrency, Name = "GetCountriesByCurrencyCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCurrencyCode(string currencyCode, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Fetching countries with currency code: {CurrencyCode}, CorrelationId: {CorrelationId}", currencyCode, correlationId);

            // Validate currencyCode
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                _logger.LogWarning("Currency code is null or empty. CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    Errors = new List<string> { "Currency code cannot be null or empty." },
                    ErrorsDetails = new List<string?> { null },
                    ErrorCodes = new List<string> { ErrorCode.InvalidInput.ToString() },
                    StatusCode = 400,
                    StatusPhrase = "Bad Request",
                    Path = HttpContext.Request.Path,
                    Method = HttpContext.Request.Method,
                    Detail = "Invalid input data"
                });
            }

            if (!currencyCode.All(char.IsLetter))
            {
                _logger.LogWarning("Currency code {CurrencyCode} contains non-letter characters. CorrelationId: {CorrelationId}", currencyCode, correlationId);
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    Errors = new List<string> { "Currency code must contain only letters." },
                    ErrorsDetails = new List<string?> { null },
                    ErrorCodes = new List<string> { ErrorCode.InvalidInput.ToString() },
                    StatusCode = 400,
                    StatusPhrase = "Bad Request",
                    Path = HttpContext.Request.Path,
                    Method = HttpContext.Request.Method,
                    Detail = "Invalid input data"
                });
            }

            if (currencyCode.Length != 3)
            {
                _logger.LogWarning("Currency code {CurrencyCode} must be exactly 3 characters long; found {Length} characters. CorrelationId: {CorrelationId}", currencyCode, currencyCode.Length, correlationId);
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    Errors = new List<string> { $"Currency code must be exactly 3 characters long; found {currencyCode.Length} characters." },
                    ErrorsDetails = new List<string?> { null },
                    ErrorCodes = new List<string> { ErrorCode.InvalidInput.ToString() },
                    StatusCode = 400,
                    StatusPhrase = "Bad Request",
                    Path = HttpContext.Request.Path,
                    Method = HttpContext.Request.Method,
                    Detail = "Invalid input data"
                });
            }

            // Convert to uppercase
            var upperCurrencyCode = currencyCode.ToUpper();
            _logger.LogDebug("Converted currency code to uppercase: {UpperCurrencyCode}", upperCurrencyCode);

            var result = await _countryService.GetByCurrencyCodeAsync(currencyCode, correlationId, cancellationToken);
            if (!result.IsSuccess)
                return HandleResult(result, correlationId);

            return Ok(result);
        }

        [HttpPut(ApiRoutes.CountryRoutes.ById, Name = "UpdateCountry")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> UpdateCountry([FromRoute] int countryId, [FromBody] CountryUpdateDto request, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Updating country with ID: {CountryId}, Request: {@Request}, CorrelationId: {CorrelationId}", countryId, request, correlationId);
            var result = await _countryService.UpdateAsync(countryId, request, correlationId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to update country. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }
            _logger.LogInformation("Successfully updated country with ID: {CountryId}, CorrelationId: {CorrelationId}", countryId, correlationId);
            return Ok(result);
        }

        [HttpDelete(ApiRoutes.CountryRoutes.ById, Name = "DeleteCountry")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int countryId, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Deleting country with ID: {CountryId}, CorrelationId: {CorrelationId}", countryId, correlationId);

            var result = await _countryService.DeleteAsync(countryId, correlationId, cancellationToken);
            if (!result.IsSuccess)
                return HandleResult(result, correlationId);

            return NoContent();
        }
    }
}
