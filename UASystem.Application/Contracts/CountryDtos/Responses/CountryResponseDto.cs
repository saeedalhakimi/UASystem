using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.CountryDtos.Responses
{
    public record CountryResponseDto
    {
        public int CountryId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Continent { get; set; }
        public string? Capital { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CountryDialNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
