using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.CountryDtos;
using UASystem.Api.Application.Contracts.CountryDtos.Responses;
using UASystem.Api.Application.Contracts.PersonDtos;
using UASystem.Api.Application.Contracts.PersonDtos.Requests;
using UASystem.Api.Application.Contracts.PersonDtos.Responses;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Application.Services.PersonServices.Commands;
using UASystem.Api.Application.Services.PersonServices.PersonSubServices;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.DomainExceptions;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Domain.ValueObjects.CountryObjects;
using UASystem.Api.Domain.ValueObjects.PersonNameValues;

namespace UASystem.Api.Application.Services.PersonServices
{
    public class PersonService : IPersonService
    {
        private readonly IPersonRepository _personRepository;
        private readonly IAppLogger<PersonService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly ICreatePersonService _createPersonService;
        public PersonService(IPersonRepository personRepository, IAppLogger<PersonService> logger, IErrorHandlingService errorHandlingService, ICreatePersonService createPersonService)
        {
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _createPersonService = createPersonService ?? throw new ArgumentNullException(nameof(createPersonService));
        }

        public async Task<OperationResult<CreatedResponseDto>> CreatePersonAsync(CreatePersonCommand request, CancellationToken cancellationToken)
        {
            var operationName = nameof(CreatePersonAsync);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", request.CorrelationId } });
            _logger.LogInformation("Handling {operationame} for {FirstName} {LastName}. CorrelationId: {CorrelationId}", operationName,
                request.FirstName, request.LastName, request.CorrelationId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with service.");

                _logger.LogInformation("Creating new person enitity with request: {@Request}, CreatedBy: {CreatedBy}, CorrelationId: {CorrelationId}", request, request.CreatedBy, request.CorrelationId);
                var person = _createPersonService.CreatePerson(request);
                //var person = PersonFactory.CreatePerson(request);

                _logger.LogDebug("Attempting to create person in repository. CreatedBy: {CreatedBy}, CorrelationId: {CorrelationId}", request.CreatedBy, request.CorrelationId);
                var isCreated = await _personRepository.CreateAsync(person, request.CreatedBy, request.CorrelationId, cancellationToken);
                if (!isCreated)
                {
                    _logger.LogWarning("person creation failed. CorrelationId: {CorrelationId}", request.CorrelationId);
                    return OperationResult<CreatedResponseDto>.Failure(new Error(
                        ErrorCode.ResourceCreationFailed,
                        "RESOURCE_CREATION_FAILED",
                        $"Failed to create person.",
                        request.CorrelationId));
                }

                var response = PersonMappers.ToCreatedResponseDto(person);
                _logger.LogInformation("Successfully executed {OperationName}. CorrelationId: {CorrelationId}", operationName, request.CorrelationId);
                return OperationResult<CreatedResponseDto>.Success(response);

            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService.HandleCancelationToken<CreatedResponseDto>(ex, request.CorrelationId);
            }
            catch (DomainModelInvalidException ex)
            {
                return _errorHandlingService.HandleDomainValidationException<CreatedResponseDto>(ex, request.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService.HandleException<CreatedResponseDto>(ex, request.CorrelationId);
            }
        }

        public async Task<OperationResult<PersonResponseDto>> GetPersonByIdAsync(Guid personId, bool includeDeleted, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<PersonResponseDto, PersonService>(
                async () =>
                {
                    _logger.LogInformation("Retrieving person with ID: {PersonId}, IncludeDeleted: {IncludeDeleted}, CorrelationId: {CorrelationId}",
                        personId, includeDeleted, correlationId);

                    var person = await _personRepository.GetByIdAsync(personId, includeDeleted, correlationId, cancellationToken);
                    if (person == null)
                    {
                        _logger.LogWarning("Person not found. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                        return null;
                    }

                    var response = person.ToPersonResponseDto();
                    _logger.LogInformation("Person retrieved successfully with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                    return response;
                },
                _logger,
                _errorHandlingService,
                "GetPersonByIdAsync",
                $"Retrieving person with ID: {personId}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<PersonResponseDto>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "PERSON_NOT_FOUND",
                    $"Person with ID {personId} not found.",
                    correlationId));
            }

            return result;
        }

        public async Task<OperationResult<UpdatedResponseDto>> UpdatePersonAsync(Guid personId, UpdatePersonDto request, Guid updatedBy, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<UpdatedResponseDto, PersonService>(
                async () =>
                {
                    // Retrieve current person to preserve fields
                    var person = await _personRepository.GetByIdAsync(personId, false, correlationId, cancellationToken);
                    if (person == null)
                    {
                        _logger.LogWarning("Person not found for update. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                        return null;
                    }

                    var oldRowVersion = person.RowVersion;

                    // Create updated person object
                    var personName = PersonName.Create(
                        request.FirstName,
                        request.MiddleName,
                        request.LastName,
                        request.Title,
                        request.Suffix
                    );

                    person.UpdateName(personName, updatedBy);
                    // Update person in repository
                    var (success, newRowVersion) = await _personRepository.UpdateAsync(person, correlationId, cancellationToken);
                    if (!success || newRowVersion == null)
                    {
                        _logger.LogWarning("Person update failed. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                        return null;
                    }

                    var response = PersonMappers.ToUpdatedResponseDto(oldRowVersion, newRowVersion, success);
                    _logger.LogInformation("Person updated successfully with ID: {PersonId}, NewRowVersion: {NewRowVersion}, CorrelationId: {CorrelationId}",
                        personId, Convert.ToBase64String(newRowVersion), correlationId);
                    return response;

                },
                _logger,
                _errorHandlingService,
                "UpdatePersonAsync",
                $"Updating person with ID: {personId}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<UpdatedResponseDto>.Failure(new Error(
                    ErrorCode.ConcurrencyConflict,
                    "CONCURRENCY_CONFLICT",
                    $"Failed to update person with ID {personId} due to concurrency conflict or record not found.",
                    correlationId));
            }

            return result;

        }
    }

}
