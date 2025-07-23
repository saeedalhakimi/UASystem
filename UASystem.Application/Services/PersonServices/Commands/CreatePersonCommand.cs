using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Services.PersonServices.Commands
{
    public class CreatePersonCommand
    {
        public string? Title { get; set; }
        public string FirstName { get; set; } = default!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = default!;
        public string? Suffix { get; set; }
        public Guid CreatedBy { get; set; }
        public string CorrelationId { get; set; } = default!;

        // Add more fields as needed
    }
}
