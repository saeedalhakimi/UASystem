using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.CountryDtos;
using UASystem.Api.Application.Contracts.CountryDtos.Requests;
using UASystem.Api.Application.Contracts.CountryDtos.Responses;
using UASystem.Api.Application.Models;

namespace UASystem.Api.Application.IServices
{
    /// <summary>
    /// Defines the contract for business logic operations related to country management in the UASystem.
    /// </summary>
    /// <remarks>
    /// This interface provides methods for creating, retrieving, updating, and deleting country records,
    /// encapsulating business rules and validation. All operations are asynchronous, support cancellation
    /// tokens, and return an <see cref="OperationResult{T}"/> to indicate success or failure with detailed
    /// error information. A correlation ID is included for request tracing and logging.
    /// </remarks>
    public interface ICountryService
    {
        /// <summary>
        /// Creates a new country record based on the provided data transfer object.
        /// </summary>
        /// <param name="request">The data transfer object containing the country details to create.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{CountryResponseDto}"/>
        /// containing the created country’s details or an error if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method validates the <paramref name="request"/>, maps it to a <see cref="Country"/> entity,
        /// and delegates persistence to the repository. It returns a <see cref="CountryResponseDto"/> with the
        /// created country’s details.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
        Task<OperationResult<CountryResponseDto>> CreateAsync(CountryCreateDto request, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing country record with the provided details.
        /// </summary>
        /// <param name="id">The unique identifier of the country to update.</param>
        /// <param name="request">The data transfer object containing the updated country details.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{CountryResponseDto}"/>
        /// containing the updated country’s details or an error if the operation fails (e.g., country not found).
        /// </returns>
        /// <remarks>
        /// This method validates the <paramref name="request"/>, retrieves the existing country, applies updates,
        /// and delegates persistence to the repository. The <c>UpdatedAt</c> timestamp is updated automatically.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
        Task<OperationResult<CountryResponseDto>> UpdateAsync(int id, CountryUpdateDto request, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all non-deleted country records.
        /// </summary>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{IReadOnlyList{CountryResponseDto}}"/>
        /// containing a read-only list of all non-deleted countries’ details or an error if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method fetches all countries where <c>IsDeleted = 0</c> and maps them to <see cref="CountryResponseDto"/>.
        /// An empty list is returned if no countries are found.
        /// </remarks>
        Task<OperationResult<IReadOnlyList<CountryResponseDto>>> GetCountriesAsync(string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its unique identifier.
        /// </summary>
        /// <param name="countryId">The unique identifier of the country to retrieve.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{CountryResponseDto}"/>
        /// containing the country’s details or an error if the country is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This method retrieves a country where <c>CountryId = @countryId</c> and <c>IsDeleted = 0</c>,
        /// mapping it to a <see cref="CountryResponseDto"/>. Returns a failure result if the country is not found.
        /// </remarks>
        Task<OperationResult<CountryResponseDto>> GetByIdAsync(int countryId, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its ISO country code.
        /// </summary>
        /// <param name="countryCode">The ISO country code (e.g., "US", "DE") to search for.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{CountryResponseDto}"/>
        /// containing the country’s details or an error if the country is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This method retrieves a country where <c>CountryCode = @countryCode</c> and <c>IsDeleted = 0</c>,
        /// mapping it to a <see cref="CountryResponseDto"/>. The country code is expected to be a 2- or 3-letter
        /// ISO 3166-1 code. Returns a failure result if the country is not found.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="countryCode"/> is null.</exception>
        Task<OperationResult<CountryResponseDto>> GetByCodeAsync(string countryCode, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its name.
        /// </summary>
        /// <param name="countryName">The name of the country to search for (e.g., "United States").</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{CountryResponseDto}"/>
        /// containing the country’s details or an error if the country is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This method retrieves a country where <c>Name = @countryName</c> and <c>IsDeleted = 0</c>,
        /// mapping it to a <see cref="CountryResponseDto"/>. The search may be case-sensitive depending on
        /// the database collation. Returns a failure result if the country is not found.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="countryName"/> is null.</exception>
        Task<OperationResult<CountryResponseDto>> GetByNameAsync(string countryName, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Soft-deletes a country record by marking it as deleted.
        /// </summary>
        /// <param name="countryId">The unique identifier of the country to delete.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{int}"/>
        /// containing the ID of the deleted country or an error if the country is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This method marks the country as deleted by setting <c>IsDeleted = 1</c> and updates the
        /// <c>UpdatedAt</c> timestamp. Returns a failure result if the country is not found.
        /// </remarks>
        Task<OperationResult<int>> DeleteAsync(int countryId, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all non-deleted country records that use the specified currency code.
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 currency code (e.g., "USD", "EUR") to search for.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning an <see cref="OperationResult{IReadOnlyList{CountryResponseDto}}"/>
        /// containing a read-only list of matching countries’ details or an error if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method retrieves all countries where <c>CurrencyCode = @currencyCode</c> and <c>IsDeleted = 0</c>,
        /// mapping them to <see cref="CountryResponseDto"/>. The currency code is expected to be a 3-letter
        /// ISO 4217 code. An empty list is returned if no matches are found.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="currencyCode"/> is null.</exception>
        Task<OperationResult<IReadOnlyList<CountryResponseDto>>> GetByCurrencyCodeAsync(string currencyCode, string? correlationId, CancellationToken cancellationToken);
    }
}
