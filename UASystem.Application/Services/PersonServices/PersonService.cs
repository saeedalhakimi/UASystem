using System;
using System.Collections;
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
using UASystem.Api.Application.Services.PersonServices.Queries;
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

        public async Task<OperationResult<CreatedResponseDto>> CreatePersonAsync(CreatePersonCommand command, CancellationToken cancellationToken)
        {
            var operationName = nameof(CreatePersonAsync);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", command.CorrelationId } });
            _logger.LogInformation("Handling {operationame} for {FirstName} {LastName}. CorrelationId: {CorrelationId}", operationName,
                command.FirstName, command.LastName, command.CorrelationId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with service.");

                _logger.LogInformation("Creating new person enitity with request: {@Request}, CreatedBy: {CreatedBy}, CorrelationId: {CorrelationId}", command, command.CreatedBy, command.CorrelationId);
                var person = _createPersonService.CreatePerson(command);
                //var person = PersonFactory.CreatePerson(request);

                _logger.LogDebug("Attempting to create person in repository. CreatedBy: {CreatedBy}, CorrelationId: {CorrelationId}", command.CreatedBy, command.CorrelationId);
                var isCreated = await _personRepository.CreateAsync(person, command.CreatedBy, command.CorrelationId, cancellationToken);
                if (!isCreated)
                {
                    _logger.LogWarning("person creation failed. CorrelationId: {CorrelationId}", command.CorrelationId);
                    return OperationResult<CreatedResponseDto>.Failure(new Error(
                        ErrorCode.ResourceCreationFailed,
                        "RESOURCE_CREATION_FAILED",
                        $"Failed to create person.",
                        command.CorrelationId));
                }

                var response = PersonMappers.ToCreatedResponseDto(person);
                _logger.LogInformation("Successfully executed {OperationName}. CorrelationId: {CorrelationId}", operationName, command.CorrelationId);
                return OperationResult<CreatedResponseDto>.Success(response);

            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService.HandleCancelationToken<CreatedResponseDto>(ex, command.CorrelationId);
            }
            catch (DomainModelInvalidException ex)
            {
                return _errorHandlingService.HandleDomainValidationException<CreatedResponseDto>(ex, command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService.HandleException<CreatedResponseDto>(ex, command.CorrelationId);
            }
        }

        public async Task<OperationResult<PersonResponseDto>> GetPersonByIdAsync(GetPersonByIdQuery query, CancellationToken cancellationToken)
        {
            var personId = query.PersonId;
            var includeDeleted = query.IncludeDeleted;
            var correlationId = query.CorrelationId;
            var operationName = nameof(GetPersonByIdAsync);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogInformation("Handling {operationame} for person with request:{@Request}.", operationName, query);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with service.");

                _logger.LogInformation("Retrieving person with ID: {PersonId}, IncludeDeleted: {IncludeDeleted}, CorrelationId: {CorrelationId}",
                        personId, includeDeleted, correlationId);
                var person = await _personRepository.GetByIdAsync(personId, includeDeleted, correlationId, cancellationToken);
                if (person == null)
                {
                    _logger.LogWarning("Person not found. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                    return OperationResult<PersonResponseDto>.Failure(new Error(
                        ErrorCode.ResourceNotFound,"PERSON_NOT_FOUND",
                        $"Person with ID {personId} not found.",
                        correlationId));
                }

                var response = person.ToPersonResponseDto();
                _logger.LogInformation("Successfully executed {OperationName}. CorrelationId: {CorrelationId}", operationName, correlationId);
                return OperationResult<PersonResponseDto>.Success(response);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService.HandleCancelationToken<PersonResponseDto>(ex, correlationId);
            }
            catch (DomainModelInvalidException ex)
            {
                return _errorHandlingService.HandleDomainValidationException<PersonResponseDto>(ex, correlationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService.HandleException<PersonResponseDto>(ex, correlationId);
            }
        }

        public async Task<OperationResult<UpdatedResponseDto>> UpdatePersonAsync(UpdatePersonCommand command, CancellationToken cancellationToken)
        {
            var personId = command.PersonId;
            var correlationId = command.CorrelationId;
            var operationName = nameof(UpdatePersonAsync);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } });
            _logger.LogInformation("Handling {operationame} for person with request:{@Request}.", operationName, command);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with service.");

                _logger.LogInformation("Retrieving person with ID: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                var person = await _personRepository.GetByIdAsync(personId, false, correlationId, cancellationToken);
                if (person == null)
                {
                    _logger.LogWarning("Person not found for update. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                    return OperationResult<UpdatedResponseDto>.Failure(new Error(
                        ErrorCode.ResourceNotFound, "PERSON_NOT_FOUND",
                        $"Person with ID {personId} not found.",
                        correlationId));
                }

                var oldRowVersion = person.RowVersion;

                // Create updated person object
                _logger.LogInformation("Creating updated person object with request: {@Request}, UpdatedBy: {UpdatedBy}, CorrelationId: {CorrelationId}",
                   command, command.UpdatedBy, correlationId);
                var personName = PersonName.Create(
                   command.FirstName,
                   command.MiddleName,
                   command.LastName,
                   command.Title,
                   command.Suffix
                );

                _logger.LogDebug("Updating person name with: {@PersonName}, CorrelationId: {CorrelationId}", personName, correlationId);
                person.UpdateName(personName, command.UpdatedBy);

                // Update person in repository
                _logger.LogDebug("Attempting to update person in repository. PersonId: {PersonId}, UpdatedBy: {UpdatedBy}, CorrelationId: {CorrelationId}",
                    personId, command.UpdatedBy, correlationId);

                var (success, newRowVersion) = await _personRepository.UpdateAsync(person, correlationId, cancellationToken);
                if (!success || newRowVersion == null)
                {
                    _logger.LogWarning("Person update failed. PersonId: {PersonId}, CorrelationId: {CorrelationId}", personId, correlationId);
                    return OperationResult<UpdatedResponseDto>.Failure(new Error(
                        ErrorCode.ConcurrencyConflict, "CONCURRENCY_CONFLICT",
                        $"Failed to update person with ID {personId} due to concurrency conflict or record not found.",
                        correlationId));
                }

                var response = PersonMappers.ToUpdatedResponseDto(oldRowVersion, newRowVersion, success);

                _logger.LogInformation("Successfully executed {OperationName}. CorrelationId: {CorrelationId}", operationName, correlationId);
                return OperationResult<UpdatedResponseDto>.Success(response);

            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService.HandleCancelationToken<UpdatedResponseDto>(ex, correlationId);
            }
            catch (DomainModelInvalidException ex)
            {
                return _errorHandlingService.HandleDomainValidationException<UpdatedResponseDto>(ex, correlationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService.HandleException<UpdatedResponseDto>(ex, correlationId);
            }
        }
    }

}
