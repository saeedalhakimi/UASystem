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
    /// Represents the result of an operation, which can either be successful or contain errors.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// Gets the payload data of the operation, or null if the operation failed.
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; }

        /// <summary>
        /// Indicates whether the operation resulted in an error.
        /// </summary>
        [JsonPropertyName("isError")]
        public bool IsError { get; }

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess => !IsError;

        /// <summary>
        /// Gets the list of errors that occurred during the operation.
        /// </summary>
        [JsonPropertyName("errors")]
        public IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets the timestamp when the operation result was created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        private OperationResult(T? data, bool isError, List<Error> errors)
        {
            Data = data;
            IsError = isError;
            Errors = errors.AsReadOnly();
        }

        // Success factory methods
        public static OperationResult<T> Success(T payload) => new(payload, false, new List<Error>());
        public static OperationResult<(IEnumerable<T1>, T2)> Success<T1, T2>(IEnumerable<T1> item1, T2 item2) =>
            new((item1, item2), false, new List<Error>());


        // Failure factory methods
        public static OperationResult<T> Failure(Error error) => new(default, true, new List<Error> { error });
        public static OperationResult<T> Failure(IReadOnlyList<Error> errors) => new(default, true, errors.ToList());
        public static OperationResult<T> Failure(ErrorCode code, string message, string? details = null, string? correlationId = null) =>
            new(default, true, new List<Error> { new Error(code, message, details, correlationId) });


        // Error inspection methods
        public bool HasErrors() => IsError && Errors.Any();
        public string GetErrorMessage() => string.Join("; ", Errors.Select(e => e.Message));
        public string GetFirstErrorMessage() => Errors.FirstOrDefault()?.Message ?? string.Empty;
        public bool HasError(ErrorCode code) => Errors.Any(e => e.Code.Equals(code));

        public override string ToString() =>
            IsError
                ? $"[{Timestamp:O}] Error(s): {GetErrorMessage()}"
                : $"[{Timestamp:O}] Success: {Data}";
    }
}
