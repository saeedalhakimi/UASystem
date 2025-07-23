namespace UASystem.Api.Routes
{
    /// <summary>
    /// Defines all centralized API routes for the application.
    /// Helps maintain consistency and avoids hardcoded route strings.
    /// </summary>
    public static class ApiRoutes
    {
        /// <summary>
        /// Base route pattern for all API controllers with versioning.
        /// </summary>
        public const string BaseRoute = "api/v{version:apiVersion}/[controller]";
        
        /// <summary>
        /// Contains route constants for the Country controller.
        /// </summary>
        public static class CountryRoutes
        {
            /// <summary>
            /// Route to get a country by its unique identifier.
            /// </summary>
            public const string ById = "{countryId}";

            /// <summary>
            /// Route to get a country by its code.
            /// </summary>
            public const string ByCode = "by-code/{countryCode}";

            /// <summary>
            /// Route to get a country by its name.
            /// </summary>
            public const string ByName = "by-name/{name}";

            /// <summary>
            /// Route to get a countries by currency.
            /// </summary>
            public const string ByCurrency = "currency/{currencyCode}";
        }

        public static class PersonRoutes
        {
            /// <summary>
            /// Route to get a person by their unique identifier.
            /// </summary>
            public const string ById = "{personId}";
            /// <summary>
            /// Route to get a person by their name.
            /// </summary>
            public const string ByName = "by-name/{name}";
            /// <summary>
            /// Route to get all persons, optionally including deleted ones.
            /// </summary>
            public const string All = "all";
        }
    }
}
