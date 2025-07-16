using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.ILoggingService
{
    /// <summary>
    /// Defines a generic logger interface for logging messages and requests with contextual scoping.
    /// </summary>
    public interface IAppLogger<T>
    {
        /// <summary>
        /// Logs an information message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Logs an error message with an associated exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        void LogError(Exception exception, string message, params object[] args);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Logs an HTTP request with a correlation ID.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., GET, POST).</param>
        /// <param name="path">The request path.</param>
        /// <param name="correlationId">The correlation ID for tracing the request.</param>
        void LogRequest(string method, string path, string correlationId);

        /// <summary>
        /// Begins a logging scope with the specified properties, which are included in all logs within the scope.
        /// </summary>
        /// <param name="properties">A dictionary of properties to include in the scope.</param>
        /// <returns>An IDisposable that ends the scope when disposed.</returns>
        IDisposable BeginScope(Dictionary<string, object> properties);
    }
}
