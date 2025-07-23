using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.ValueObjects.CountryObjects;

namespace UASystem.Api.Domain.Entities.CountryIntity
{
    /// <summary>
    /// Represents a country entity that encapsulates a unique identifier and detailed information.
    /// </summary>
    /// <remarks>
    /// This class is used to store and manage a country’s identity and metadata, such as name, code,
    /// continent, capital, currency, and dialing code. Validation is enforced at creation and reconstruction
    /// to ensure data integrity. The entity supports soft deletion and tracks creation and update timestamps.
    /// </remarks>
    public class Country
    {
        /// <summary>
        /// Gets the unique identifier for the country.
        /// </summary>
        /// <remarks>
        /// This is a positive integer assigned by the database upon creation and is immutable.
        /// </remarks>
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the detailed metadata of the country.
        /// </summary>
        /// <remarks>
        /// This includes properties like the country name, ISO code, continent, currency, and dialing code,
        /// encapsulated in a <see cref="CountryDetails"/> value object. The setter is private to enforce
        /// updates through the <see cref="UpdateDetails"/> method.
        /// </remarks>
        public CountryDetails Details { get; private set; }

        /// <summary>
        /// Gets the timestamp indicating when this record was created.
        /// </summary>
        /// <remarks>
        /// This is set to the current UTC time during creation and is immutable thereafter.
        /// </remarks>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the timestamp indicating when this record was last updated, if any.
        /// </summary>
        /// <remarks>
        /// This is null upon creation and updated to the current UTC time when <see cref="UpdateDetails"/>,
        /// <see cref="MarkAsDeleted"/>, or <see cref="MarkAsActive"/> is called.
        /// </remarks>
        public DateTime? UpdatedAt { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this record has been soft-deleted.
        /// </summary>
        /// <remarks>
        /// A value of <c>true</c> indicates the country is marked as deleted and should not appear in standard queries.
        /// Use <see cref="MarkAsDeleted"/> or <see cref="MarkAsActive"/> to modify this state.
        /// </remarks>
        public bool IsDeleted { get; private set; }

        /// <summary>
        /// Private constructor to prevent direct instantiation.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Create"/> or <see cref="Reconstruct"/> to instantiate a <see cref="Country"/> object.
        /// </remarks>
        private Country() { }

        /// <summary>
        /// Creates a new <see cref="Country"/> instance with the specified details.
        /// </summary>
        /// <param name="details">The detailed metadata for the country, including name, code, and other attributes.</param>
        /// <returns>
        /// A new <see cref="Country"/> instance with the provided details, initialized creation timestamp,
        /// and <c>IsDeleted</c> set to <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method sets <see cref="CreatedAt"/> to the current UTC time and ensures the country is marked
        /// as active. The <see cref="Id"/> is typically set by the database after persistence.
        /// </remarks>
        /// <exception cref="CountryNotValidException">Thrown if <paramref name="details"/> is null.</exception>
        public static Country Create(CountryDetails details)
        {
            DomainValidator.ThrowIfObjectNull("CountryDetails", details, new CountryNotValidException("Country details cannot be null."));

            return new Country
            {
                Details = details,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
            };
        }

        /// <summary>
        /// Reconstructs a <see cref="Country"/> instance from persisted values (e.g., from a database).
        /// </summary>
        /// <param name="countryId">The unique identifier for the country.</param>
        /// <param name="details">The existing detailed information of the country.</param>
        /// <param name="createdAt">The timestamp when the country was created.</param>
        /// <param name="updatedAt">The timestamp when the country was last updated, if applicable.</param>
        /// <param name="isDeleted">Indicates whether the country record is marked as deleted.</param>
        /// <returns>
        /// A reconstructed <see cref="Country"/> object using historical data.
        /// </returns>
        /// <remarks>
        /// This method is typically used to recreate domain entities from storage without triggering new creation
        /// logic. It ensures the entity is fully populated with persisted values, including soft-deletion status.
        /// </remarks>
        /// <exception cref="CountryNotValidException">
        /// Thrown if <paramref name="countryId"/> is not a positive integer or if <paramref name="details"/> is null.
        /// </exception>
        public static Country Reconstruct(int countryId, CountryDetails details, DateTime createdAt, DateTime? updatedAt, bool isDeleted)
        {
            DomainValidator.ThrowIfIdNotPositive("CountryId", countryId, new CountryNotValidException("Country ID must be a positive integer."));
            DomainValidator.ThrowIfObjectNull("CountryDetails", details, new CountryNotValidException("Country details cannot be null."));

            return new Country
            {
                Id = countryId,
                Details = details,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                IsDeleted = isDeleted
            };
        }

        /// <summary>
        /// Updates the country’s metadata with new details.
        /// </summary>
        /// <param name="newDetails">The new detailed metadata to apply to the country.</param>
        /// <remarks>
        /// This method updates the <see cref="Details"/> property and sets <see cref="UpdatedAt"/> to the
        /// current UTC time. It does not modify the <see cref="Id"/> or <see cref="CreatedAt"/> properties.
        /// </remarks>
        /// <exception cref="CountryNotValidException">Thrown if <paramref name="newDetails"/> is null.</exception>
        public void UpdateDetails(CountryDetails newDetails)
        {
            DomainValidator.ThrowIfObjectNull("NewCountryDetails", newDetails, new CountryNotValidException("New country details cannot be null."));

            Details = newDetails;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the country as soft-deleted.
        /// </summary>
        /// <remarks>
        /// This method sets <see cref="IsDeleted"/> to <c>true</c> and updates <see cref="UpdatedAt"/> to the
        /// current UTC time. The country remains in the database but is excluded from standard queries.
        /// </remarks>
        public void MarkAsDeleted()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the country as active, undoing a soft deletion.
        /// </summary>
        /// <remarks>
        /// This method sets <see cref="IsDeleted"/> to <c>false</c> and updates <see cref="UpdatedAt"/> to the
        /// current UTC time, making the country visible in standard queries again.
        /// </remarks>
        public void MarkAsActive()
        {
            IsDeleted = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
