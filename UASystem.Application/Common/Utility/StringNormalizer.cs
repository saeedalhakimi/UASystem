using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Common.Utility
{
    public static class StringNormalizer
    {
        public static string? Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
