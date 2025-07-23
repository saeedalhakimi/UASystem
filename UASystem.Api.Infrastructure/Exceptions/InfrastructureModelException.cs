using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Infrastructure.Exceptions
{
    internal class InfrastructureModelException : Exception
    {
        internal InfrastructureModelException()
        {
            ValidationErrors = new List<string>();
        }

        internal InfrastructureModelException(string message) : base(message)
        {
            ValidationErrors = new List<string>();
        }

        internal InfrastructureModelException(string message, Exception inner) : base(message, inner)
        {
            ValidationErrors = new List<string>();
        }
        public List<string> ValidationErrors { get; }
    }
}
