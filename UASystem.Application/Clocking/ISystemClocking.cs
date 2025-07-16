using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Clocking
{
    /// <summary>
    /// Provides access to the current system time, enabling testability.
    /// </summary>
    public interface ISystemClocking
    {
        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}
