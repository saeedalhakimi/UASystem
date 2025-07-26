using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UASystem.Api.Domain.DomainExceptions.PersonExceptions;
using UASystem.Api.Domain.DomainExceptions.UserExceptions;

namespace UASystem.Api.Domain.ValueObjects.UserValues
{
    public record UserName
    {
        private const int MaxLength = 50;
        public string Value { get; private init; }
        public string NormalizedValue => Value.ToUpperInvariant();

        private UserName(string value)
        {
            Value = value;
        }

        public static UserName Create(string value)
        {
            DomainValidator.ThrowIfStringNullOrEmpty("Username", value, new UserNotValidException("Username cannot be empty.."));
            DomainValidator.ThrowIfExceedsMaxLength("Username", value, MaxLength, new UserNotValidException($"Username cannot exceed {MaxLength} characters."));
            
            if (!Regex.IsMatch(value, @"^[a-zA-Z0-9_-]+$"))
                throw new UserNotValidException("Username can only contain letters, numbers, underscores, and hyphens.");

            return new UserName(value);
        }
    }
}
