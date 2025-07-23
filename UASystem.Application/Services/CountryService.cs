using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.CountryDtos;
using UASystem.Api.Application.Contracts.CountryDtos.Requests;
using UASystem.Api.Application.Contracts.CountryDtos.Responses;
using UASystem.Api.Application.Enums;
using UASystem.Api.Application.Exceptions;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Application.IServices;
using UASystem.Api.Application.Models;
using UASystem.Api.Domain.DomainExceptions;
using UASystem.Api.Domain.Entities.CountryIntity;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Domain.ValueObjects.CountryObjects;

namespace UASystem.Api.Application.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IAppLogger<CountryService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        public CountryService(
            ICountryRepository countryRepository,
            IAppLogger<CountryService> logger,
            IErrorHandlingService errorHandlingService)
        {
            _countryRepository = countryRepository ?? throw new ArgumentNullException(nameof(countryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        public async Task<OperationResult<IReadOnlyList<CountryResponseDto>>> GetCountriesAsync(string? correlationId, CancellationToken cancellationToken)
        {
            return await ServiceOperationHandler.ExecuteAsync<IReadOnlyList<CountryResponseDto>, CountryService>(
               async () =>
               {
                   var countries = await _countryRepository.GetAllAsync(correlationId, cancellationToken);
                   return countries.Select(CountryMappers.ToResponseDto).ToList().AsReadOnly() as IReadOnlyList<CountryResponseDto>;
               },
               _logger,
               _errorHandlingService,
               "GetAllCountriesAsync",
               "Fetching all countries",
               correlationId,
               cancellationToken);
        }
        public async Task<OperationResult<CountryResponseDto>> GetByIdAsync(int countryId, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<CountryResponseDto, CountryService>(
                async () =>
                {
                    var country = await _countryRepository.GetByIdAsync(countryId, correlationId, cancellationToken);
                    return country != null ? CountryMappers.ToResponseDto(country) : null;
                },
                _logger,
                _errorHandlingService,
                "GetByIdAsync",
                $"Fetching country with ID {countryId}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<CountryResponseDto>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "RESOURCE_NOT_FOUND",
                    $"Country with ID {countryId} not found",
                    correlationId));
            }

            return result;
        }
        public async Task<OperationResult<CountryResponseDto>> GetByCodeAsync(string countryCode, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<CountryResponseDto, CountryService>(
                async () =>
                {
                    var country = await _countryRepository.GetByCodeAsync(countryCode, correlationId, cancellationToken);
                    return country != null ? CountryMappers.ToResponseDto(country) : null;
                },
                _logger,
                _errorHandlingService,
                "GetByCodeAsync",
                $"Fetching country with Code {countryCode}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<CountryResponseDto>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "RESOURCE_NOT_FOUND",
                    $"Country with Code {countryCode} not found",
                    correlationId));
            }

            return result;
        }
        public async Task<OperationResult<CountryResponseDto>> GetByNameAsync(string countryName, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<CountryResponseDto, CountryService>(
                async () =>
                {
                    var country = await _countryRepository.GetByNameAsync(countryName, correlationId, cancellationToken);
                    return country != null ? CountryMappers.ToResponseDto(country) : null;
                },
                _logger,
                _errorHandlingService,
                "GetByNameAsync",
                $"Fetching country with name {countryName}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<CountryResponseDto>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "RESOURCE_NOT_FOUND",
                    $"Country with name {countryName} not found",
                    correlationId));
            }

            return result;
        }
        public async Task<OperationResult<CountryResponseDto>> CreateAsync(CountryCreateDto request, string? correlationId, CancellationToken cancellationToken)
        {
            return await ServiceOperationHandler.ExecuteAsync<CountryResponseDto, CountryService>(
                async () =>
                {
                    //// Validate required fields
                    //if (string.IsNullOrWhiteSpace(request.Name))
                    //{
                    //    _logger.LogWarning("Country name is empty. CorrelationId: {CorrelationId}", correlationId);
                    //    throw new DomainModelInvalidException("Country name is required.");
                    //}

                    //if (string.IsNullOrWhiteSpace(request.CountryCode))
                    //{
                    //    _logger.LogWarning("Country code is empty. CorrelationId: {CorrelationId}", correlationId);
                    //    throw new DomainModelInvalidException("Country code is required.");
                    //}

                    //if (!System.Text.RegularExpressions.Regex.IsMatch(request.CountryCode, @"^[A-Z]{2,3}$"))
                    //{
                    //    _logger.LogWarning("Country code must be 2 or 3 uppercase letters: {CountryCode}. CorrelationId: {CorrelationId}", request.CountryCode, correlationId);
                    //    throw new DomainModelInvalidException("Country code must be 2 or 3 uppercase letters.");
                    //}

                    //if (request.CountryDialNumber != null && !System.Text.RegularExpressions.Regex.IsMatch(request.CountryDialNumber, @"^\+[0-9]+$"))
                    //{
                    //    _logger.LogWarning("Country dial number must start with '+' followed by digits: {CountryDialNumber}. CorrelationId: {CorrelationId}", request.CountryDialNumber, correlationId);
                    //    throw new DomainModelInvalidException("Country dial number must start with '+' followed by digits.");
                    //}

                    var countryDetails = CountryDetails.Create(
                        request.CountryCode,
                        request.Name,
                        request.Continent,
                        request.Capital,
                        request.CurrencyCode,
                        request.CountryDialNumber);
                    var country = Country.Create(countryDetails);

                    int countryId = await _countryRepository.CreateAsync(country, correlationId, cancellationToken);
                    var responseDto = CountryMappers.ToResponseDto(country);
                    responseDto.CountryId = countryId; // Set the ID from the repository
                    return responseDto;
                },
                _logger,
                _errorHandlingService,
                "CreateAsync",
                $"Creating country with name {request.Name}",
                correlationId,
                cancellationToken);
        }
        public async Task<OperationResult<CountryResponseDto>> UpdateAsync(int id, CountryUpdateDto request, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<CountryResponseDto, CountryService>(
                async () =>
                {
                    var country = await _countryRepository.GetByIdAsync(id, correlationId, cancellationToken);
                    if (country == null)
                    {
                        _logger.LogWarning("Country with ID {CountryId} not found. CorrelationId: {CorrelationId}", id, correlationId);
                        return null;
                    }

                    var countryUpdatedDetails = CountryDetails.Create(
                        request.CountryCode,
                        request.Name,
                        request.Continent,
                        request.Capital,
                        request.CurrencyCode,
                        request.CountryDialNumber);

                    country.UpdateDetails(countryUpdatedDetails);
                    await _countryRepository.UpdateAsync(country, correlationId, cancellationToken);
                    return CountryMappers.ToResponseDto(country);
                },
                _logger,
                _errorHandlingService,
                "UpdateAsync",
                $"Updating country with ID {id}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == null)
            {
                return OperationResult<CountryResponseDto>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "RESOURCE_NOT_FOUND",
                    $"Country with ID {id} not found",
                    correlationId));
            }

            return result;
        }
        public async Task<OperationResult<int>> DeleteAsync(int countryId, string? correlationId, CancellationToken cancellationToken)
        {
            var result = await ServiceOperationHandler.ExecuteAsync<int, CountryService>(
                async () =>
                {
                    var country = await _countryRepository.GetByIdAsync(countryId, correlationId, cancellationToken);
                    if (country == null)
                    {
                        _logger.LogWarning("Country with ID {CountryId} not found. CorrelationId: {CorrelationId}", countryId, correlationId);
                        return 0;
                    }
                    country.MarkAsDeleted();
                    var returnedCountryId = await _countryRepository.DeleteAsync(countryId, correlationId, cancellationToken);
                    return returnedCountryId;
                },
                _logger,
                _errorHandlingService,
                "DeleteAsync",
                $"Deleting country with ID {countryId}",
                correlationId,
                cancellationToken);

            if (result.IsSuccess && result.Data == 0)
            {
                return OperationResult<int>.Failure(new Error(
                    ErrorCode.ResourceNotFound,
                    "RESOURCE_NOT_FOUND",
                    $"Country with ID {countryId} not found",
                    correlationId));
            }

            return result;
        }
        public async Task<OperationResult<IReadOnlyList<CountryResponseDto>>> GetByCurrencyCodeAsync(string currencyCode, string? correlationId, CancellationToken cancellationToken)
        {
            return await ServiceOperationHandler.ExecuteAsync<IReadOnlyList<CountryResponseDto>, CountryService>(
                async () =>
                {
                    var countries = await _countryRepository.GetByCurrencyCodeAsync(currencyCode, correlationId, cancellationToken);
                    return countries.Select(CountryMappers.ToResponseDto).ToList().AsReadOnly() as IReadOnlyList<CountryResponseDto>;
                },
                _logger,
                _errorHandlingService,
                "GetByCurrencyCodeAsync",
                $"Fetching countries with currency code {currencyCode}",
                correlationId,
                cancellationToken);
        }
    }

}
