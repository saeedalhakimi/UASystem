using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.PersonDtos.Responses
{
    public record PersonResponseDto
    {
        public Guid PersonId { get; init; }
        public string? Title { get; set; } 
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string? Suffix { get; set; }
        public DateTime CreatedAt { get; init; }
        public Guid? CreatedBy { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public Guid? UpdatedBy { get; init; }
        public bool IsDeleted { get; init; }
        public DateTime? DeletedAt { get; init; }
        public Guid? DeletedBy { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }
}
