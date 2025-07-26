using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Services.PersonServices.Queries
{
    public class GetAllPersonsQuery
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; } 
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public bool IncludeDeleted { get; set; }
        public string? CorrelationId { get; set; }

    }
}
