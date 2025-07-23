using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Infrastructure.Exceptions.PersonExceptions
{
    internal class PersonRepositoryException : InfrastructureModelException
    {
        internal PersonRepositoryException() { }
        internal PersonRepositoryException(string message) : base(message) { }
        internal PersonRepositoryException(string message, Exception inner) : base(message, inner) { }
    }
}
