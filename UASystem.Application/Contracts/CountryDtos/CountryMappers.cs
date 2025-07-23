using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Application.Contracts.CountryDtos.Responses;
using UASystem.Api.Domain.Entities.CountryIntity;

namespace UASystem.Api.Application.Contracts.CountryDtos
{
    public static class CountryMappers
    {
        public static CountryResponseDto ToResponseDto(Country country)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));
            return new CountryResponseDto
            {
                CountryId = country.Id,
                CountryCode = country.Details.CountryCode,
                Name = country.Details.Name,
                Continent = country.Details.Continent,
                Capital = country.Details.Capital,
                CurrencyCode = country.Details.CurrencyCode,
                CountryDialNumber = country.Details.CountryDialNumber,
                CreatedAt = country.CreatedAt,
                UpdatedAt = country.UpdatedAt,
                IsDeleted = country.IsDeleted
            };
        }
    }
}
