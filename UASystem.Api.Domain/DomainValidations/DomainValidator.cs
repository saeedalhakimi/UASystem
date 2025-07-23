using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UASystem.Api.Domain.DomainExceptions;

namespace UASystem.Api.Domain.DomainValidations
{
    /// <summary>
    /// Provides centralized domain-level validation methods for value checks and business rules.
    /// </summary>
    public static class DomainValidator
    {

        private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        /// <summary>
        /// Throws the specified exception if the email format is invalid.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <param name="ex">The exception to throw if the format is invalid.</param>
        public static void ThrowIfInvalidEmailFormat(string email, Exception ex)
        {
            if (!Regex.IsMatch(email, EmailPattern))
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the object is null.
        /// </summary>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="value">The object value to check.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfObjectNull(string field, object? value, Exception ex)
        {
            if (value is null)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the string value is null or empty.
        /// </summary>
        /// <param name="field" > The name of the field being validated.</param>
        /// <param name="value"> The string value to check.</param>
        /// <param name="ex"> The exception to throw if validation fails.</param>
        public static void ThrowIfStringNullOrEmpty(string field, string? value, Exception ex)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the Guid value is empty.
        /// </summary>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="value">The Guid value to check.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfEmptyGuid(string field, Guid value, Exception ex)
        {
            if (value == Guid.Empty)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the entity is marked as deleted.
        /// </summary>
        /// <param name="isDeleted">Indicates whether the entity is deleted.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfDeleted(bool isDeleted, Exception ex)
        {
            if (isDeleted)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if a duplicate ID is found in the list.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="id">The ID to check for duplication.</param>
        /// <param name="list">The list of existing entities.</param>
        /// <param name="idSelector">Function to select the ID from each entity.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfDuplicate<T>(string field, Guid id, IEnumerable<T> list, Func<T, Guid> idSelector, Exception ex)
        {
            if (list.Any(item => idSelector(item) == id))
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the entity's PersonId does not match the expected value.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="entity">The entity to validate.</param>
        /// <param name="expectedPersonId">The expected PersonId to match.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfMismatch<T>(string field, T entity, Guid expectedPersonId, Exception ex) where T : class
        {
            if (entity is null)
                throw new DomainModelInvalidException($"{field} cannot be null.");

            var personIdProp = typeof(T).GetProperty("PersonId")?.GetValue(entity);
            if (personIdProp is not Guid actualId || actualId != expectedPersonId)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the string exceeds the maximum allowed length.
        /// </summary>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="value">The string value to check.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfExceedsMaxLength(string field, string value, int maxLength, Exception ex)
        {
            if (value is not null && value.Length > maxLength)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if a provided optional string is empty or exceeds the maximum allowed length.
        /// </summary>
        /// <param name="field">The name of the field being validated.</param>
        /// <param name="value">The string value to check.</param>
        /// <param name="maxLength">The maximum allowed length, if any.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfProvidedButEmpty(string field, string? value, int? maxLength, Exception ex)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw ex;

            if (maxLength.HasValue && value.Length > maxLength.Value)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the birth date is in the future.
        /// </summary>
        /// <param name="birthDate">The birth date to validate.</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfBirthDateInFuture(DateTime birthDate, Exception ex)
        {
            if (birthDate > DateTime.UtcNow)
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the birth date is earlier than the allowed minimum date (default is 01-Jan-1900).
        /// </summary>
        /// <param name="birthDate">The birth date to validate.</param>
        /// <param name="minDate">The earliest allowed date (defaults to 1900-01-01).</param>
        /// <param name="ex">The exception to throw if validation fails.</param>
        public static void ThrowIfBirthDateTooOld(DateTime birthDate, DateTime? minDate, Exception ex)
        {
            if (birthDate < (minDate ?? new DateTime(1900, 1, 1)))
                throw ex;
        }

        /// <summary>
        /// Throws the specified exception if the provided ID is not positive.
        /// </summary>  
        /// <param name="field"> The name of the field being validated.</param>
        /// <param name="value"> The ID value to check.</param>
        /// <param name="ex"> The exception to throw if validation fails.</param>
        public static void ThrowIfIdNotPositive(string field, int value, Exception ex)
        {
            if (value <= 0)
                throw ex;
        }

    }
}
