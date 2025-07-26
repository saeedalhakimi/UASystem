using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Services.PersonServices.Commands
{
    public class DeletePersonCommand
    {
        public Guid PersonId { get; set; }
        public Guid DeletedBy { get; set; }
        public string? CorrelationId { get; set; }
    }
}
