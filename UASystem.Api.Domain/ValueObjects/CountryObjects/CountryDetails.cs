namespace UASystem.Api.Domain.ValueObjects.CountryObjects
{
    /// <summary>
    /// Represents detailed information about a country, including its code, name, continent, capital,
    /// currency, and dialing code.
    /// </summary>
    /// <remarks>
    /// This record is used to encapsulate basic geographical and political information about a country
    /// in a structured and validated form.
    /// </remarks>
    public record CountryDetails
    {
        private const int MaxCountryAndCurrencyCodeLength = 3;
        private const int MaxDialNumber = 5;
        private const int MaxNameLength = 100;

        /// <summary>
        /// Gets the ISO 3166-1 alpha-3 code of the country (e.g., "USA", "GBR").
        /// </summary>
        public string CountryCode { get; private set; }

        /// <summary>
        /// Gets the name of the country (e.g., "United States").
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the continent the country belongs to (e.g., "North America").
        /// </summary>
        public string? Continent { get; private set; }

        /// <summary>
        /// Gets the name of the capital city of the country.
        /// </summary>
        public string? Capital { get; private set; }

        /// <summary>
        /// Gets the ISO 4217 currency code used in the country (e.g., "USD").
        /// </summary>
        public string? CurrencyCode { get; private set; }

        /// <summary>
        /// Gets the international dialing code prefix for the country (e.g., "+1").
        /// </summary>
        public string? CountryDialNumber { get; private set; }


        private CountryDetails(string countryCode, string name, string? continent, string? capital, string? currencyCode, string? countryDialNumber)
        {
            CountryCode = countryCode;
            Name = name;
            Continent = continent;
            Capital = capital;
            CurrencyCode = currencyCode;
            CountryDialNumber = countryDialNumber;
        }

        /// <summary>
        /// Creates a new <see cref="CountryDetails"/> instance after validating all inputs.
        /// </summary>
        /// <param name="countryCode">The ISO country code (required, max 3 characters).</param>
        /// <param name="name">The name of the country (required, max 100 characters).</param>
        /// <param name="continent">Optional name of the continent (max 100 characters).</param>
        /// <param name="capital">Optional name of the capital city (max 100 characters).</param>
        /// <param name="currencyCode">Optional ISO currency code (max 3 characters).</param>
        /// <param name="countryDialNumber">Optional international dialing code (max 5 characters).</param>
        /// <returns>
        /// A validated <see cref="CountryDetails"/> instance containing the specified information.
        /// </returns>
        /// <remarks>
        /// All parameters are validated against length constraints, and required fields must not be null or empty.
        /// </remarks>
        /// <exception cref="CountryNotValidException">
        /// Thrown if any required field is null, empty, or if any field exceeds its defined maximum length.
        /// </exception>
        public static CountryDetails Create(string countryCode, string name, string? continent = null, string? capital = null, string? currencyCode = null, string? countryDialNumber = null)
        {
            DomainValidator.ThrowIfStringNullOrEmpty("CountryCode", countryCode, new CountryNotValidException("Country code cannot be null or empty."));
            DomainValidator.ThrowIfStringNullOrEmpty("Name", name, new CountryNotValidException("Country name cannot be null or empty."));
            DomainValidator.ThrowIfExceedsMaxLength("CountryCode", countryCode, MaxCountryAndCurrencyCodeLength, new CountryNotValidException($"Country code cannot exceed {MaxCountryAndCurrencyCodeLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Name", name, MaxNameLength, new CountryNotValidException($"Country name cannot exceed {MaxNameLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Continent", continent, MaxNameLength, new CountryNotValidException($"Continent cannot exceed {MaxNameLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("Capital", capital, MaxNameLength, new CountryNotValidException($"Capital cannot exceed {MaxNameLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("CurrencyCode", currencyCode, MaxCountryAndCurrencyCodeLength, new CountryNotValidException($"Currency code cannot exceed {MaxCountryAndCurrencyCodeLength} characters."));
            DomainValidator.ThrowIfExceedsMaxLength("CountryDialNumber", countryDialNumber, MaxDialNumber, new CountryNotValidException($"Country dial number cannot exceed {MaxDialNumber} characters."));

            return new CountryDetails(countryCode, name, continent, capital, currencyCode, countryDialNumber);
        }
    }
}
