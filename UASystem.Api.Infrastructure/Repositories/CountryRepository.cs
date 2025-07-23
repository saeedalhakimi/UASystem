using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Domain.DomainExceptions;
using UASystem.Api.Domain.Entities.CountryIntity;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Domain.ValueObjects.CountryObjects;
using UASystem.Api.Infrastructure.Data.IDataWrapperFactory;

namespace UASystem.Api.Infrastructure.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppLogger<CountryRepository> _logger;
        private readonly string _connectionString;
        public CountryRepository(IConfiguration configuration,
            IDatabaseConnectionFactory connectionFactory,
            IAppLogger<CountryRepository> logger,
            string connectionString = null)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<int> CreateAsync(Country country, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting create process for country: {CountryName}", country.Details.Name);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.SP_CreateCountry";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@CountryCode", country.Details.CountryCode);
                command.AddParameter("@Name", country.Details.Name);
                command.AddParameter("@Continent", (object?)country.Details.Continent ?? DBNull.Value);
                command.AddParameter("@Capital", (object?)country.Details.Capital ?? DBNull.Value);
                command.AddParameter("@CurrencyCode", (object?)country.Details.CurrencyCode ?? DBNull.Value);
                command.AddParameter("@CountryDialNumber", (object?)country.Details.CountryDialNumber ?? DBNull.Value);
                command.AddParameter("@CorrelationId", (object?)correlationId ?? DBNull.Value);

                // Output parameter
                command.AddOutputParameter("@CountryId", SqlDbType.Int);

                _logger.LogDebug("Parameters added to stored procedure: CountryCode={CountryCode}, Name={Name}, Continent={Continent}, Capital={Capital}, CurrencyCode={CurrencyCode}, CountryDialNumber={CountryDialNumber}, CorrelationId={CorrelationId}",
                    country.Details.CountryCode, country.Details.Name, country.Details.Continent, country.Details.Capital, country.Details.CurrencyCode, country.Details.CountryDialNumber, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                await command.ExecuteNonQueryAsync(cancellationToken);
                var countryId = Convert.ToInt32(command.GetParameterValue("@CountryId"));
                _logger.LogInformation("Successfully created country with ID {CountryId}", countryId);

                return countryId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country: {CountryName}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    country.Details.Name, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<int> UpdateAsync(Country country, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting update process for country: {CountryName} (ID: {CountryId})", country.Details.Name, country.Id);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.SP_UpdateCountry";
                command.CommandType = CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@CountryId", country.Id);
                command.AddParameter("@CountryCode", country.Details.CountryCode);
                command.AddParameter("@Name", country.Details.Name);
                command.AddParameter("@Continent", (object?)country.Details.Continent ?? DBNull.Value);
                command.AddParameter("@Capital", (object?)country.Details.Capital ?? DBNull.Value);
                command.AddParameter("@CurrencyCode", (object?)country.Details.CurrencyCode ?? DBNull.Value);
                command.AddParameter("@CountryDialNumber", (object?)country.Details.CountryDialNumber ?? DBNull.Value);
                command.AddParameter("@CorrelationId", (object?)correlationId ?? DBNull.Value);

                _logger.LogDebug("Parameters added to stored procedure: CountryId={CountryId}, CountryCode={CountryCode}, Name={Name}, Continent={Continent}, Capital={Capital}, CurrencyCode={CurrencyCode}, CountryDialNumber={CountryDialNumber}, CorrelationId={CorrelationId}",
                    country.Id, country.Details.CountryCode, country.Details.Name, country.Details.Continent, country.Details.Capital, country.Details.CurrencyCode, country.Details.CountryDialNumber, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var countryId = Convert.ToInt32(result);
                _logger.LogInformation("Successfully updated country with ID {CountryId}", countryId);

                return countryId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating country: {CountryName} (ID: {CountryId}). Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    country.Details.Name, country.Id, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<IReadOnlyList<Country>> GetAllAsync(string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting fetch process for all countries");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_GetAllCountries";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.AddParameter("@CorrelationId", correlationId ?? (object)DBNull.Value);
                _logger.LogDebug("Parameters added to stored procedure: CorrelationId={CorrelationId}", correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var countries = new List<Country>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var countryDetails = CountryDetails.Create(
                        reader.GetString(reader.GetOrdinal("CountryCode")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        reader.IsDBNull(reader.GetOrdinal("Continent")) ? null : reader.GetString(reader.GetOrdinal("Continent")),
                        reader.IsDBNull(reader.GetOrdinal("Capital")) ? null : reader.GetString(reader.GetOrdinal("Capital")),
                        reader.IsDBNull(reader.GetOrdinal("CurrencyCode")) ? null : reader.GetString(reader.GetOrdinal("CurrencyCode")),
                        reader.IsDBNull(reader.GetOrdinal("CountryDialNumber")) ? null : reader.GetString(reader.GetOrdinal("CountryDialNumber")));

                    var country = Country.Reconstruct(
                        reader.GetInt32(reader.GetOrdinal("CountryID")),
                        countryDetails,
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")), 
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")));

                    countries.Add(country);
                }

                _logger.LogInformation("Fetched {Count} countries successfully", countries.Count);
                return countries.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"An unexpected error occurred while fetching countries. CorrelationId: {CorrelationId}", correlationId);
                throw; // Re-throw to allow higher-level handling
            }
        }
        public async Task<Country?> GetByCodeAsync(string countryCode, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting fetch process for country by code: {CountryCode}", countryCode);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_GetCountryByCode";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.AddParameter("@CountryCode", countryCode);
                command.AddParameter("@CorrelationId", correlationId ?? (object)DBNull.Value);
                _logger.LogDebug("Parameters added to stored procedure: CountryCode={CountryCode}, CorrelationId={CorrelationId}", countryCode, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var countryDetails = CountryDetails.Create(
                        reader.GetString(reader.GetOrdinal("CountryCode")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        reader.IsDBNull(reader.GetOrdinal("Continent")) ? null : reader.GetString(reader.GetOrdinal("Continent")),
                        reader.IsDBNull(reader.GetOrdinal("Capital")) ? null : reader.GetString(reader.GetOrdinal("Capital")),
                        reader.IsDBNull(reader.GetOrdinal("CurrencyCode")) ? null : reader.GetString(reader.GetOrdinal("CurrencyCode")),
                        reader.IsDBNull(reader.GetOrdinal("CountryDialNumber")) ? null : reader.GetString(reader.GetOrdinal("CountryDialNumber")));
                    var country = Country.Reconstruct(
                        reader.GetInt32(reader.GetOrdinal("CountryID")),
                        countryDetails,
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")));
                    _logger.LogInformation("Fetched country with code {CountryCode} successfully", countryCode);
                    return country;
                }

                _logger.LogWarning("No country found with code {CountryCode}", countryCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching country by ID: {CountryId}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    countryCode, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<Country?> GetByIdAsync(int countryId, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting fetch process for country by ID: {CountryId}", countryId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_GetCountryById";
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.AddParameter("@CountryID", countryId);
                command.AddParameter("@CorrelationId", correlationId ?? (object)DBNull.Value);
                _logger.LogDebug("Parameters added to stored procedure: CountryID={CountryId}, CorrelationId={CorrelationId}", countryId, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var countryDetails = CountryDetails.Create(
                        reader.GetString(reader.GetOrdinal("CountryCode")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        reader.IsDBNull(reader.GetOrdinal("Continent")) ? null : reader.GetString(reader.GetOrdinal("Continent")),
                        reader.IsDBNull(reader.GetOrdinal("Capital")) ? null : reader.GetString(reader.GetOrdinal("Capital")),
                        reader.IsDBNull(reader.GetOrdinal("CurrencyCode")) ? null : reader.GetString(reader.GetOrdinal("CurrencyCode")),
                        reader.IsDBNull(reader.GetOrdinal("CountryDialNumber")) ? null : reader.GetString(reader.GetOrdinal("CountryDialNumber")));

                    var country = Country.Reconstruct(
                        reader.GetInt32(reader.GetOrdinal("CountryID")),
                        countryDetails,
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")));

                    _logger.LogInformation("Fetched country with ID {CountryId} successfully", countryId);
                    return country;
                }

                _logger.LogWarning("No country found with ID {CountryId}", countryId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching country by ID: {CountryId}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    countryId, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<Country?> GetByNameAsync(string name, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting fetch process for country by name: {Name}", name);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_GetCountryByName";
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.AddParameter("@Name", name ?? throw new ArgumentNullException(nameof(name)));
                command.AddParameter("@CorrelationId", correlationId ?? (object)DBNull.Value);
                _logger.LogDebug("Parameters added to stored procedure: Name={Name}, CorrelationId={CorrelationId}", name, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var countryDetails = CountryDetails.Create(
                        reader.GetString(reader.GetOrdinal("CountryCode")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        reader.IsDBNull(reader.GetOrdinal("Continent")) ? null : reader.GetString(reader.GetOrdinal("Continent")),
                        reader.IsDBNull(reader.GetOrdinal("Capital")) ? null : reader.GetString(reader.GetOrdinal("Capital")),
                        reader.IsDBNull(reader.GetOrdinal("CurrencyCode")) ? null : reader.GetString(reader.GetOrdinal("CurrencyCode")),
                        reader.IsDBNull(reader.GetOrdinal("CountryDialNumber")) ? null : reader.GetString(reader.GetOrdinal("CountryDialNumber")));

                    var country = Country.Reconstruct(
                        reader.GetInt32(reader.GetOrdinal("CountryID")),
                        countryDetails,
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")));

                    _logger.LogInformation("Fetched country with name {name} successfully", name);
                    return country;
                }

                _logger.LogWarning("No country found with name {name}", name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching country by name: {name}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    name, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<int> DeleteAsync(int countryId, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting delete process for country with ID: {CountryId}", countryId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.SP_DeleteCountry";
                command.CommandType = CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@CountryId", countryId);
                command.AddParameter("@CorrelationId", (object?)correlationId ?? DBNull.Value);

                _logger.LogDebug("Parameters added to stored procedure: CountryId={CountryId}, CorrelationId={CorrelationId}",
                    countryId, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var returnedCountryId = Convert.ToInt32(result);
                _logger.LogInformation("Successfully deleted country with ID {CountryId}", returnedCountryId);

                return returnedCountryId;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting country with ID: {CountryId}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    countryId, ex.GetType().Name, correlationId);
                throw;
            }
        }
        public async Task<IReadOnlyList<Country>> GetByCurrencyCodeAsync(string currencyCode, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting fetch process for countries by currency code: {CurrencyCode}", currencyCode);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.SP_GetCountriesByCurrencyCode";
                command.CommandType = CommandType.StoredProcedure;

                command.AddParameter("@CurrencyCode", currencyCode);
                command.AddParameter("@CorrelationId", (object?)correlationId ?? DBNull.Value);
                _logger.LogDebug("Parameters added to stored procedure: CurrencyCode={CurrencyCode}, CorrelationId={CorrelationId}", currencyCode, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened successfully.");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var countries = new List<Country>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    var countryDetails = CountryDetails.Create(
                        reader.GetString(reader.GetOrdinal("CountryCode")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        reader.IsDBNull(reader.GetOrdinal("Continent")) ? null : reader.GetString(reader.GetOrdinal("Continent")),
                        reader.IsDBNull(reader.GetOrdinal("Capital")) ? null : reader.GetString(reader.GetOrdinal("Capital")),
                        reader.IsDBNull(reader.GetOrdinal("CurrencyCode")) ? null : reader.GetString(reader.GetOrdinal("CurrencyCode")),
                        reader.IsDBNull(reader.GetOrdinal("CountryDialNumber")) ? null : reader.GetString(reader.GetOrdinal("CountryDialNumber")));

                    var country = Country.Reconstruct(
                        reader.GetInt32(reader.GetOrdinal("CountryId")),
                        countryDetails,
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")));

                    countries.Add(country);
                }

                _logger.LogInformation("Fetched {Count} countries with currency code {CurrencyCode} successfully", countries.Count, currencyCode);
                return countries.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries by currency code: {CurrencyCode}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    currencyCode, ex.GetType().Name, correlationId);
                throw;
            }
        }
    }
}
