using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;

namespace UASystem.Api.Domain.ValueObjects.PersonNameValues
{
    /// <summary>
    /// Represents a person's full name, including optional middle name, title, and suffix.
    /// </summary>
    /// <remarks>
    /// This value object encapsulates structured name components with validation rules applied during creation.
    /// Only <c>FirstName</c> and <c>LastName</c> are required; other fields are optional. All string values are
    /// trimmed, and maximum length constraints are enforced for all parts of the name.
    /// </remarks>
    public sealed record PersonName
    {
        /// <summary>
        /// The maximum allowed length for any individual name component.
        /// </summary>
        private const int MaxLength = 50;

        /// <summary>
        /// Gets the person's first name. This field is required.
        /// </summary>
        public string FirstName { get; init; }

        /// <summary>
        /// Gets the person's middle name. This field is optional.
        /// </summary>
        public string? MiddleName { get; init; }

        /// <summary>
        /// Gets the person's last name. This field is required.
        /// </summary>
        public string LastName { get; init; }

        /// <summary>
        /// Gets the person's title (e.g., Mr., Dr.). This field is optional.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the person's suffix (e.g., Jr., Sr.). This field is optional.
        /// </summary>
        public string? Suffix { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonName"/> record.
        /// </summary>
        /// <param name="firstName">The person's first name. Required and must not exceed maximum length.</param>
        /// <param name="middleName">The person's middle name. Optional and must not exceed maximum length.</param>
        /// <param name="lastName">The person's last name. Required and must not exceed maximum length.</param>
        /// <param name="title">The person's title. Optional.</param>
        /// <param name="suffix">The person's suffix. Optional.</param>
        private PersonName(string firstName, string? middleName, string lastName, string? title, string? suffix)
        {
            FirstName = firstName;
            MiddleName = middleName;
            LastName = lastName;
            Title = title;
            Suffix = suffix;
        }

        /// <summary>
        /// Creates a new instance of <see cref="PersonName"/> with validated name components.
        /// </summary>
        /// <remarks>
        /// This factory method validates each name component according to business rules. 
        /// The <paramref name="firstName"/> and <paramref name="lastName"/> are required and must not exceed the maximum allowed length.
        /// The <paramref name="middleName"/>, <paramref name="title"/>, and <paramref name="suffix"/> are optional, 
        /// but if provided, they are trimmed and validated against the same length constraint.
        /// </remarks>
        /// <param name="firstName">The person's first name. Required and must not exceed maximum length.</param>
        /// <param name="middleName">The person's middle name. Optional and must not exceed maximum length.</param>
        /// <param name="lastName">The person's last name. Required and must not exceed maximum length.</param>
        /// <param name="title">The person's title. Optional.</param>
        /// <param name="suffix">The person's suffix. Optional.</param>
        /// <returns>A new validated <see cref="PersonName"/> instance.</returns>
        /// <exception cref="PersonNameNotValidException">
        /// Thrown if required fields are null/empty or if any name component exceeds the allowed length.
        /// </exception>
        public static PersonName Create(string firstName, string? middleName, string lastName, string? title, string? suffix)
        {
            DomainValidator.ThrowIfStringNullOrEmpty("First name", firstName, new PersonNotValidException("First name is required."));
            DomainValidator.ThrowIfExceedsMaxLength("First name", firstName, MaxLength, new PersonNotValidException($"First name cannot exceed {MaxLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Middle name", middleName, MaxLength, new PersonNotValidException($"Middle name cannot exceed {MaxLength} characters."));
            DomainValidator.ThrowIfStringNullOrEmpty("Last name", lastName, new PersonNotValidException("Last name is required."));
            DomainValidator.ThrowIfExceedsMaxLength("Last name", lastName, MaxLength, new PersonNotValidException($"Last name cannot exceed {MaxLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Title", title, MaxLength, new PersonNotValidException($"Title cannot exceed {MaxLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Suffix", suffix, MaxLength, new PersonNotValidException($"Suffix cannot exceed {MaxLength} characters."));

            return new PersonName(firstName.Trim(), middleName?.Trim(), lastName.Trim(), title?.Trim(), suffix?.Trim());
        }

        /// <summary>
        /// Returns the full name as a formatted string including title, first name, middle name, last name, and suffix.
        /// </summary>
        /// <returns>A formatted full name string.</returns>
        public override string ToString()
        {
            return $"{Title} {FirstName} {MiddleName} {LastName} {Suffix}".Replace("  ", " ").Trim();
        }
    }
}
