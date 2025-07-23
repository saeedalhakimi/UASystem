using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Domain.DomainExceptions
{
    public class DomainModelInvalidException : Exception
    {
        public DomainModelInvalidException()
        {
            ValidationErrors = new List<string>();
        }

        public DomainModelInvalidException(string message) : base(message)
        {
            ValidationErrors = new List<string>();
        }

        public DomainModelInvalidException(string message, Exception inner) : base(message, inner)
        {
            ValidationErrors = new List<string>();
        }
        public List<string> ValidationErrors { get; }
    }
}
