using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Contracts.PersonDtos.Responses
{
    public record UpdatedResponseDto
    {
        public bool IsSuccess { get; set; }
        public byte[] OldVersion { get; set; } = Array.Empty<byte>();
        public byte[] NewVersion { get; set; } = Array.Empty<byte>();
    }
}
