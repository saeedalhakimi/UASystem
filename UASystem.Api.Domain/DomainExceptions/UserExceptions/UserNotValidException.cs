using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Domain.DomainExceptions.UserExceptions
{
    public class UserNotValidException : DomainModelInvalidException
    {
        internal UserNotValidException() { }
        internal UserNotValidException(string message) : base(message) { }
        internal UserNotValidException(string message, Exception inner) : base(message, inner) { }
    }
}
