using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.PersonDtos.Responses
{
    public record CreatedResponseDto
    {
        public Guid PersonId { get; init; }
        public string? Title { get; set; } 
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string? Suffix { get; set; }
        public DateTime CreatedAt { get; init; }
        public Guid? CreatedBy { get; init; }
    }
}
