using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Clocking
{
    /// <summary>
    /// Provides the current system time in UTC.
    /// </summary>
    public class SystemClocking : ISystemClocking
    {
        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
