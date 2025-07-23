using System.ComponentModel.DataAnnotations;

namespace UASystem.Api.Application.Contracts.CountryDtos
{
    public record CountryCreateDto
    {
        /// <summary>
        /// Gets or sets the ISO 3166-1 alpha-2 or alpha-3 code of the country (e.g., "US", "USA").
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("countryCode")]
        [Required(ErrorMessage = "Country code is required.")]
        [RegularExpression(@"^[A-Z]{2,3}$", ErrorMessage = "Country code must be 2 or 3 uppercase letters.")]
        public string CountryCode { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the country (e.g., "United States").
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [Required(ErrorMessage = "Country name is required.")]
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the continent the country belongs to (e.g., "North America").
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("continent")]
        [MaxLength(50, ErrorMessage = "Continent name cannot exceed 50 characters.")]
        public string? Continent { get; init; }

        /// <summary>
        /// Gets or sets the name of the capital city of the country.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("capital")]
        [MaxLength(100, ErrorMessage = "Capital name cannot exceed 100 characters.")]
        public string? Capital { get; init; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code used in the country (e.g., "USD").
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("currencyCode")]
        [MaxLength(3, ErrorMessage = "Currency code must be 3 uppercase letters.")]
        public string? CurrencyCode { get; init; }

        /// <summary>
        /// Gets or sets the international dialing code prefix for the country (e.g., "+1").
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("countryDialNumber")]
        [RegularExpression(@"^\+[0-9]+$", ErrorMessage = "Country dial number must start with '+' followed by digits.")]
        public string? CountryDialNumber { get; init; }
    }
}