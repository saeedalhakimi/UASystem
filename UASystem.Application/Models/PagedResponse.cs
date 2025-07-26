﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Models
{
    public class PagedResponse<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public List<T> Data { get; set; }

        public int? CurrentPageSize { get; set; }

        public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalCount, int? currentPageSize = 0)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Data = data;
            CurrentPageSize = currentPageSize;
        }
    }
}
