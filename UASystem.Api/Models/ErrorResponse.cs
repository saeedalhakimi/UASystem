namespace UASystem.Api.Models
{
    /// <summary>
    /// Represents an error response returned by the API.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status phrase corresponding to the status code.
        /// </summary>
        public string? StatusPhrase { get; set; }

        /// <summary>
        /// Gets or sets the list of error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of detailed error descriptions (optional).
        /// </summary>
        public List<string>? ErrorsDetails { get; set; }

        /// <summary>
        /// Gets or sets the list of error codes (optional).
        /// </summary>
        public List<string>? ErrorCodes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error occurred (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request path that caused the error.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method of the request.
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets an optional detailed message or suggestion for the client.
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking the request across systems.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}
