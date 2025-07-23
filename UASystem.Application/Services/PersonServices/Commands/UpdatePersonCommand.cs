using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Services.PersonServices.Commands
{
    public class UpdatePersonCommand
    {
        public Guid PersonId { get; set; }
        public string? Title { get; set; }
        public string FirstName { get; set; } = default!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = default!;
        public string? Suffix { get; set; }
        public Guid UpdatedBy { get; set; }
        public string CorrelationId { get; set; } = default!;
    }
}
