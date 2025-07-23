using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.ILoggingService;
using UASystem.Api.Domain.Aggregate;
using UASystem.Api.Domain.DomainExceptions;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;
using UASystem.Api.Domain.Repositories;
using UASystem.Api.Infrastructure.Data.IDataWrapperFactory;
using UASystem.Api.Infrastructure.Exceptions.PersonExceptions;

namespace UASystem.Api.Infrastructure.Repositories
{
    public class PersonRepository : IPersonRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppLogger<PersonRepository> _logger;
        private readonly string _connectionString;
        public PersonRepository(IConfiguration configuration, IDatabaseConnectionFactory connectionFactory, IAppLogger<PersonRepository> logger, string connectionString = null)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? configuration.GetConnectionString("DefaultConnection")!;
        }
        public async Task<bool> CreateAsync(Person person, Guid createdBy, string? correlationId, CancellationToken cancellationToken)
        {
            var operationName = nameof(CreateAsync);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting {Operation} in repository for person: {FullName}, correlationId: {Correlation}", operationName, $"{person.Name.FirstName} {person.Name.LastName}", correlationId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection. correlationId: {Correlation}", correlationId);

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_CreatePerson";
                command.CommandType = CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@PersonId", person.PersonId);
                command.AddParameter("@FirstName", person.Name.FirstName);
                command.AddParameter("@MiddleName", (object?)person.Name.MiddleName ?? DBNull.Value);
                command.AddParameter("@LastName", person.Name.LastName);
                command.AddParameter("@Title", (object?)person.Name.Title ?? DBNull.Value);
                command.AddParameter("@Suffix", (object?)person.Name.Suffix ?? DBNull.Value);
                command.AddParameter("@CreatedBy", (object?)createdBy ?? DBNull.Value);
                command.AddParameter("correlationId", (object?)correlationId ?? DBNull.Value);

                // Output parameters
                command.AddOutputParameter("@RowsAffected", SqlDbType.Int);

                _logger.LogDebug("Stored procedure parameters prepared: PersonId={PersonId}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, Title={Title}, Suffix={Suffix}, CreatedBy={CreatedBy}, correlationId={CorrelationId}",
                person.PersonId, person.Name.FirstName, person.Name.MiddleName, person.Name.LastName, person.Name.Title, person.Name.Suffix, person.CreatedBy, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection established. correlationId: {Correlation}", correlationId);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var rowsAffected = Convert.ToInt32(command.GetParameterValue("@RowsAffected"));

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Person created successfully. RowsAffected={RowsAffected}, PersonId={PersonId}, correlationId: {Correlation}", rowsAffected, person.PersonId, correlationId);
                    return true;
                }

                _logger.LogWarning("Create operation completed, but no rows were affected. PersonId={PersonId}, RowsAffected={RowsAffected} correlationId: {Correlation}", person.PersonId, rowsAffected, correlationId);
                return false;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Operation was cancelled during person creation. CorrelationId={CorrelationId}", correlationId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating person: {FullName}. ExceptionType={ExceptionType}, CorrelationId={CorrelationId}",
                    $"{person.Name.FirstName} {person.Name.LastName}", ex.GetType().Name, correlationId);
                throw;
            }
        }

        public async Task<Person?> GetByIdAsync(Guid personId, bool includeDeleted, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting retrieval of person with ID: {PersonId}, IncludeDeleted: {IncludeDeleted}", personId, includeDeleted);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_GetPersonById";
                command.CommandType = CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@PersonId", personId);
                command.AddParameter("@IncludeDeleted", includeDeleted);
                command.AddParameter("@CorrelationId", (object?)correlationId ?? DBNull.Value);

                _logger.LogDebug("Parameters added to stored procedure: PersonId={PersonId}, IncludeDeleted={IncludeDeleted}, CorrelationId={CorrelationId}",
                    personId, includeDeleted, correlationId);

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection established successfully.");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var person = Person.Reconstruct(
                        reader.GetGuid(reader.GetOrdinal("PersonId")),
                        reader.GetString(reader.GetOrdinal("FirstName")),
                        reader.IsDBNull(reader.GetOrdinal("MiddleName")) ? null : reader.GetString(reader.GetOrdinal("MiddleName")),
                        reader.GetString(reader.GetOrdinal("LastName")),
                        reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                        reader.IsDBNull(reader.GetOrdinal("Suffix")) ? null : reader.GetString(reader.GetOrdinal("Suffix")),
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.GetGuid(reader.GetOrdinal("CreatedBy")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("UpdatedBy")),
                        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        reader.IsDBNull(reader.GetOrdinal("DeletedBy")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("DeletedBy")),
                        reader.IsDBNull(reader.GetOrdinal("DeletedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DeletedAt")),
                        reader.GetBinary(reader.GetOrdinal("RowVersion"))
                        );

                    _logger.LogInformation("Person retrieved successfully. PersonId: {PersonId}", personId);
                    return person;
                }

                _logger.LogWarning("No person found with ID: {PersonId}. IncludeDeleted: {IncludeDeleted}", personId, includeDeleted);
                return null;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Operation canceled while retrieving person with ID: {PersonId}. CorrelationId: {CorrelationId}", personId, correlationId);
                throw;
            }
            catch (SqlException ex) when (ex.Number is 50005 or 50008)
            {
                _logger.LogError(ex, "Validation error retrieving person with ID: {PersonId}. Error: {ErrorMessage}, CorrelationId: {CorrelationId}",
                    personId, ex.Message, correlationId);
                throw new PersonRepositoryException(ex.Message, ex);
            }
            catch (DomainModelInvalidException ex)
            {
                _logger.LogError(ex, "Domain model validation error retrieving person with ID: {PersonId}. Error: {ErrorMessage}, CorrelationId: {CorrelationId}",
                    personId, ex.Message, correlationId);
                throw new PersonRepositoryException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving person with ID: {PersonId}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    personId, ex.GetType().Name, correlationId);
                throw;
            }
        }

        public async Task<(bool, byte[]?)> UpdateAsync(Person person, string? correlationId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId ?? "N/A" } });
            _logger.LogInformation("Starting update process for person: {FirstName} {LastName}, PersonId: {PersonId}", person.Name.FirstName, person.Name.LastName, person.PersonId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Cancellation token checked. Proceeding with database connection.");

                await using var connection = await _connectionFactory.CreateConnectionAsync(_connectionString, cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SP_UpdatePerson";
                command.CommandType = CommandType.StoredProcedure;

                // Input parameters
                command.AddParameter("@PersonId", person.PersonId);
                command.AddParameter("@FirstName", person.Name.FirstName);
                command.AddParameter("@MiddleName", (object?)person.Name.MiddleName ?? DBNull.Value);
                command.AddParameter("@LastName", person.Name.LastName);
                command.AddParameter("@Title", (object?)person.Name.Title ?? DBNull.Value);
                command.AddParameter("@Suffix", (object?)person.Name.Suffix ?? DBNull.Value);
                command.AddParameter("@UpdatedBy", (object?)person.UpdatedBy ?? DBNull.Value);
                command.AddParameter("correlationId", (object?)correlationId ?? DBNull.Value);
                command.AddParameter("@RowVersion", person.RowVersion);

                // Output parameter
                command.AddOutputParameter("@NewRowVersion", SqlDbType.Binary, 8);

                _logger.LogDebug("Parameters added to stored procedure: PersonId={PersonId}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, Title={Title}, Suffix={Suffix}, UpdatedBy={UpdatedBy}, CorrelationId={CorrelationId}, RowVersion={RowVersion}",
                    person.PersonId, person.Name.FirstName, person.Name.MiddleName, person.Name.LastName, person.Name.Title, person.Name.Suffix, person.UpdatedBy, correlationId, Convert.ToBase64String(person.RowVersion));

                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection established successfully.");

                await command.ExecuteNonQueryAsync(cancellationToken);
                var newRowVersion = command.GetParameterValue("@NewRowVersion") as byte[];
                var success = newRowVersion != null;

                if (success)
                {
                    _logger.LogInformation("Person updated successfully. PersonId: {PersonId}, NewRowVersion: {NewRowVersion}", person.PersonId, Convert.ToBase64String(newRowVersion));
                }
                else
                {
                    _logger.LogWarning("No rows affected during person update. PersonId: {PersonId}", person.PersonId);
                }

                return (success, newRowVersion);

            }
            catch (SqlException ex) when (ex.Number is 50001 or 50002 or 50005 or 50006 or 50007)
            {
                _logger.LogError(ex, "Validation error updating person: {FirstName} {LastName}. Error: {ErrorMessage}, CorrelationId: {CorrelationId}",
                    person.Name.FirstName, person.Name.LastName, ex.Message, correlationId);
                throw new PersonRepositoryException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating person: {FirstName} {LastName}. Type: {ExceptionType}, CorrelationId: {CorrelationId}",
                    person.Name.FirstName, person.Name.LastName, ex.GetType().Name, correlationId);
                throw;
            }
        }
    }
}
