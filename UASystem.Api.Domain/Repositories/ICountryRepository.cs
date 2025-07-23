using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.Entities.CountryIntity;

namespace UASystem.Api.Domain.Repositories
{
    /// <summary>
    /// Defines the contract for data access operations related to country entities in the UASystem.
    /// </summary>
    /// <remarks>
    /// This interface provides methods for creating, retrieving, updating, and deleting country records
    /// in the data store, typically a SQL database. All operations are asynchronous and support cancellation
    /// tokens for graceful operation termination. A correlation ID is included for request tracing and logging.
    /// The repository interacts with stored procedures to ensure data consistency and integrity.
    /// </remarks>
    public interface ICountryRepository
    {
        /// <summary>
        /// Creates a new country record in the data store.
        /// </summary>
        /// <param name="country">The country entity containing detailed metadata to be persisted.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the ID of the newly created country.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_CreateCountry</c>) to insert a new country
        /// record and returns the generated <c>CountryId</c>. Validation of the country details is assumed to be handled
        /// by the <see cref="Country"/> entity before calling this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="country"/> is null.</exception>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<int> CreateAsync(Country country, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all non-deleted country records from the data store.
        /// </summary>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning a read-only list of all non-deleted
        /// <see cref="Country"/> entities.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_GetAllCountries</c>) to fetch all countries
        /// where <c>IsDeleted = 0</c>. The result is an empty list if no countries are found.
        /// </remarks>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<IReadOnlyList<Country>> GetAllAsync(string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its unique identifier.
        /// </summary>
        /// <param name="countryId">The unique identifier of the country to retrieve.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the <see cref="Country"/> entity if found,
        /// or null if no matching non-deleted country exists.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_GetCountryById</c>) to fetch a country
        /// where <c>CountryId = @countryId</c> and <c>IsDeleted = 0</c>.
        /// </remarks>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<Country?> GetByIdAsync(int countryId, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its ISO country code.
        /// </summary>
        /// <param name="countryCode">The ISO country code (e.g., "US", "DE") to search for.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the <see cref="Country"/> entity if found,
        /// or null if no matching non-deleted country exists.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_GetCountryByCode</c>) to fetch a country
        /// where <c>CountryCode = @countryCode</c> and <c>IsDeleted = 0</c>. The country code is expected to be
        /// a 2- or 3-letter ISO 3166-1 code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="countryCode"/> is null.</exception>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<Country?> GetByCodeAsync(string countryCode, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a country record by its name.
        /// </summary>
        /// <param name="name">The name of the country to search for (e.g., "United States").</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the <see cref="Country"/> entity if found,
        /// or null if no matching non-deleted country exists.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_GetCountryByName</c>) to fetch a country
        /// where <c>Name = @name</c> and <c>IsDeleted = 0</c>. The search may be case-sensitive depending on the
        /// database collation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<Country?> GetByNameAsync(string name, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing country record with new details.
        /// </summary>
        /// <param name="country">The country entity with updated metadata to persist.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the ID of the updated country.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_UpdateCountry</c>) to update the country
        /// record identified by <c>country.Id</c>. The <c>UpdatedAt</c> timestamp is set to the current time.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="country"/> is null.</exception>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<int> UpdateAsync(Country country, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Soft-deletes a country record by marking it as deleted.
        /// </summary>
        /// <param name="countryId">The unique identifier of the country to delete.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning the ID of the deleted country.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_DeleteCountry</c>) to set
        /// <c>IsDeleted = 1</c> and update the <c>UpdatedAt</c> timestamp for the specified country.
        /// </remarks>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<int> DeleteAsync(int countryId, string? correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all non-deleted country records that use the specified currency code.
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 currency code (e.g., "USD", "EUR") to search for.</param>
        /// <param name="correlationId">A unique identifier for tracing the request, or null if not provided.</param>
        /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, returning a read-only list of <see cref="Country"/>
        /// entities that match the specified currency code.
        /// </returns>
        /// <remarks>
        /// This method typically invokes a stored procedure (e.g., <c>SP_GetCountriesByCurrencyCode</c>) to fetch
        /// all countries where <c>CurrencyCode = @currencyCode</c> and <c>IsDeleted = 0</c>. The currency code
        /// is expected to be a 3-letter ISO 4217 code. An empty list is returned if no matches are found.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="currencyCode"/> is null.</exception>
        /// <exception cref="SqlException">Thrown if a database error occurs during the operation.</exception>
        Task<IReadOnlyList<Country>> GetByCurrencyCodeAsync(string currencyCode, string? correlationId, CancellationToken cancellationToken);
    }
}
