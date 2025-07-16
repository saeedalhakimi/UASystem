using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.ILoggingService;

namespace UASystem.Api.Infrastructure.LoggingService
{
    /// <summary>
    /// Implements a generic logger using Serilog, supporting contextual scoping and request logging.
    /// </summary>
    public class AppLogger<T> : IAppLogger<T>
    {

        private readonly Serilog.ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the AppLogger, configured for the specified type.
        /// </summary>
        /// <param name="logger">The underlying Serilog logger.</param>
        public AppLogger(Serilog.ILogger logger)
        {
            // Automatically enrich logs with the SourceContext property for class name
            _logger = logger?.ForContext<T>() ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs an information message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        public void LogInformation(string message, params object[] args)
        {
            _logger.Information(message, args);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        public void LogWarning(string message, params object[] args)
        {
            _logger.Warning(message, args);
        }

        /// <summary>
        /// Logs an error message with an associated exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.Error(exception, message, args);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message template.</param>
        /// <param name="args">The arguments to format the message.</param>
        public void LogDebug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        /// <summary>
        /// Logs an HTTP request with a correlation ID.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., GET, POST).</param>
        /// <param name="path">The request path.</param>
        /// <param name="correlationId">The correlation ID for tracing the request.</param>
        public void LogRequest(string method, string path, string correlationId)
        {
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.Information("HTTP {Method} request to {Path}", method, path);
            }
        }

        /// <summary>
        /// Begins a logging scope with the specified properties, which are included in all logs within the scope.
        /// </summary>
        /// <param name="properties">A dictionary of properties to include in the scope.</param>
        /// <returns>An IDisposable that ends the scope when disposed.</returns>
        public IDisposable BeginScope(Dictionary<string, object> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var disposables = new List<IDisposable>();
            foreach (var property in properties)
            {
                disposables.Add(LogContext.PushProperty(property.Key, property.Value));
            }

            return new DisposableScope(disposables);
        }

        /// <summary>
        /// A helper class to manage multiple disposable LogContext properties.
        /// </summary>
        private class DisposableScope : IDisposable
        {
            private readonly List<IDisposable> _disposables;

            public DisposableScope(List<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
