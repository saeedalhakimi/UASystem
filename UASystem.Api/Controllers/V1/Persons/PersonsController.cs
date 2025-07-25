﻿using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UASystem.Api.Application.Common.Utility;
using UASystem.Api.Application.Contracts.PersonDtos.Requests;
using UASystem.Api.Application.Contracts.PersonDtos.Responses;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Application.Services.PersonServices;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Application.Services.PersonServices.Queries;
using UASystem.Api.Filters;
using UASystem.Api.Routes;

namespace UASystem.Api.Controllers.V1.Persons
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.BaseRoute)]
    [ApiController]
    public class PersonsController : BaseController<PersonsController>
    {
        private readonly IAppLogger<PersonsController> _logger;
        private readonly IPersonService _personService;
        public PersonsController(IAppLogger<PersonsController> logger, IPersonService personService) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        }

        [HttpGet(Name = "GetAllPersons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllPersons([FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving all persons with PageNumber: {PageNumber}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SortBy: {SortBy}, SortDescending: {SortDescending}, IncludeDeleted: {IncludeDeleted}, CorrelationId: {CorrelationId}",
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, includeDeleted, correlationId);

            if (pageNumber < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination parameters. PageNumber: {PageNumber}, PageSize: {PageSize}, CorrelationId: {CorrelationId}", pageNumber, pageSize, correlationId);
                return BadRequest("INVALID_PAGINATION_PARAMETERS, PageNumber and PageSize must be greater than 0.");
            }

            var query = new GetAllPersonsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortDescending = sortDescending,
                IncludeDeleted = includeDeleted,
                CorrelationId = correlationId
            };

            var result = await _personService.GetAllPersonsAsync(query, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve persons. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }
            _logger.LogInformation("Persons retrieved successfully, Count: {Count}, CorrelationId: {CorrelationId}", result.Data.Data.Count, correlationId);
            return Ok(result);
        }


        [HttpPost(Name = "CreatePerson")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePersonDto request, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Creating Person with request: {@Request}, CorrelationId: {CorrelationId}", request, correlationId);

            //place holder

            var createdBy = "be5a79ea-5765-f011-adb3-94c691b4234b"; // Replace with actual user ID from context or authentication

            var command = PersonFactory.CreatePersonCommandFromDto(request, Guid.Parse(createdBy), correlationId);
            var result = await _personService.CreatePersonAsync(command, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to create person. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Person created successfully with ID: {PersonId}, CorrelationId: {CorrelationId}", result.Data?.PersonId, correlationId);
            return CreatedAtRoute("GetPersonById", new { personId = result.Data?.PersonId }, result);
        }

        [HttpGet(ApiRoutes.PersonRoutes.ById, Name = "GetPersonById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("personId")]
        public async Task<IActionResult> GetPersonById([FromRoute] string personId, [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Retrieving person with ID: {PersonId}, IncludeDeleted: {IncludeDeleted}, CorrelationId: {CorrelationId}",
                personId, includeDeleted, correlationId);

            var query = new GetPersonByIdQuery
            {
                PersonId = Guid.Parse(personId),
                CorrelationId = correlationId,
                IncludeDeleted = includeDeleted
            };
            var result = await _personService.GetPersonByIdAsync(query, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to retrieve person. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }

            _logger.LogInformation("Person retrieved successfully with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
            return Ok(result);
        }

        [HttpPut(ApiRoutes.PersonRoutes.ById, Name = "UpdatePerson")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("personId")]
        [ValidateModel]
        public async Task<IActionResult> UpdatePerson([FromRoute] string personId, [FromBody] UpdatePersonDto dto, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Updating person with ID: {PersonId}, Request: {@Request}, CorrelationId: {CorrelationId}",
                personId, dto, correlationId);
            var updatedBy = "be5a79ea-5765-f011-adb3-94c691b4234b"; // Replace with actual user ID from context or authentication

            var command = PersonFactory.CreateUpdatePersonCommandFromDto(Guid.Parse(personId), dto, Guid.Parse(updatedBy), correlationId);
            var result = await _personService.UpdatePersonAsync(command, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to update person. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }
            _logger.LogInformation("Person updated successfully with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
            return Ok(result);
        }

        [HttpDelete(ApiRoutes.PersonRoutes.ById, Name = "DeletePerson")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("personId")]
        public async Task<IActionResult> DeletePerson([FromRoute] string personId, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogRequest(HttpContext.Request.Method, HttpContext.Request.Path, correlationId);
            _logger.LogInformation("Deleting person with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
            
            var deletedBy = "be5a79ea-5765-f011-adb3-94c691b4234b"; // Replace with actual user ID from context or authentication

            var command = new DeletePersonCommand
            {
                PersonId = Guid.Parse(personId),
                DeletedBy = Guid.Parse(deletedBy),
                CorrelationId = correlationId
            };

            var result = await _personService.DeletePersonAsync(command, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(null, "Failed to delete person. Errors: {Errors}, CorrelationId: {CorrelationId}",
                    string.Join(", ", result.Errors.Select(e => e.Message)), correlationId);
                return HandleResult(result, correlationId);
            }
            _logger.LogInformation("Person deleted successfully with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
            return NoContent();
        }
    }
}
