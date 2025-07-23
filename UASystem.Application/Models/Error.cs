using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UASystem.Api.Application.Enums;

namespace UASystem.Api.Application.Models
{
    /// <summary>
    /// Represents an error that occurred during an operation with additional metadata.
    /// </summary>
    public sealed class Error : IEquatable<Error>
    {
        /// <summary>
        /// Gets the error code, which uniquely identifies the error type.
        /// </summary>
        [JsonPropertyName("code")]
        public ErrorCode Code { get; }

        /// <summary>
        /// Gets the human-readable error message, potentially localized.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; }

        /// <summary>
        /// Gets additional details about the error, if available.
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; }

        /// <summary>
        /// Gets the correlation ID for tracing the error across systems.
        /// </summary>
        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; }

        /// <summary>
        /// Gets the timestamp when the error was created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">Additional details about the error, if any.</param>
        /// <param name="correlationId">A correlation ID for tracing, if any.</param>
        public Error(ErrorCode code, string message, string? details = null, string? correlationId = null)
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Details = details;
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Returns a string representation of the error.
        /// </summary>
        public override string ToString() =>
            string.IsNullOrEmpty(Details)
                ? $"[{Timestamp:O}] Error {Code}: {Message}"
                : $"[{Timestamp:O}] Error {Code}: {Message} - Details: {Details}";

        public bool Equals(Error? other)
        {
            if (other is null) return false;
            return Code.Equals(other.Code) && Message == other.Message && Details == other.Details;
        }

        public override bool Equals(object? obj) => Equals(obj as Error);
        public override int GetHashCode() => HashCode.Combine(Code, Message, Details);
    }
}
