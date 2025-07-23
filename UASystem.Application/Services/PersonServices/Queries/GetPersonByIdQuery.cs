using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Services.PersonServices.Queries
{
    public class GetPersonByIdQuery
    {
        public Guid PersonId { get; set; }
        public string? CorrelationId { get; set; }
        public bool IncludeDeleted { get; set; }

    }
}
