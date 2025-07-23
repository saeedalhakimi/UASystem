using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Exceptions
{
    public class ApplicationModelInvalidException : Exception
    {
        internal ApplicationModelInvalidException()
        {
            ValidationErrors = new List<string>();
        }

        internal ApplicationModelInvalidException(string message) : base(message)
        {
            ValidationErrors = new List<string>();
        }

        internal ApplicationModelInvalidException(string message, Exception inner) : base(message, inner)
        {
            ValidationErrors = new List<string>();
        }
        public List<string> ValidationErrors { get; }
    }
}
