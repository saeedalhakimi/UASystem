using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Domain.DomainExceptions.PersonExceptions
{
    public class PersonNotValidException : DomainModelInvalidException
    {
        internal PersonNotValidException() { }
        internal PersonNotValidException(string message) : base(message) { }
        internal PersonNotValidException(string message, Exception inner) : base(message, inner) { }
    }
}
