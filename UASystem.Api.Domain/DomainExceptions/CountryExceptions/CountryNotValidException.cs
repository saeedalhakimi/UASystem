using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Domain.DomainExceptions.CountryExceptions
{
    public class CountryNotValidException : DomainModelInvalidException
    {
        internal CountryNotValidException() { }
        internal CountryNotValidException(string message) : base(message) { }
        internal CountryNotValidException(string message, Exception inner) : base(message, inner){}
    }
}
