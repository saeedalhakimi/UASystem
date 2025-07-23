using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.CountryDtos.Requests
{
    public record CountryUpdateDto
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
        [Required(ErrorMessage = "Country name is required.")]
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the continent the country belongs to (e.g., "North America").
        /// </summary>
        public string? Continent { get; init; }

        /// <summary>
        /// Gets or sets the name of the capital city of the country.
        /// </summary>
        public string? Capital { get; init; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code used in the country (e.g., "USD").
        /// </summary>
        public string? CurrencyCode { get; init; }

        /// <summary>
        /// Gets or sets the international dialing code prefix for the country (e.g., "+1").
        /// </summary>
        [RegularExpression(@"^\+[0-9]+$", ErrorMessage = "Country dial number must start with '+' followed by digits.")]
        public string? CountryDialNumber { get; init; }
    }
}
