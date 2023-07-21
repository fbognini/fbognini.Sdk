using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Models
{
    
    public class ApiResult
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
        public IDictionary<string, string[]>? Validations { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    public class ApiResult<TResponse> : ApiResult
    {
        public TResponse? Response { get; set; }
    }

    public class PaginationResponse<TClass>
    {
        public List<TClass> Items { get; set; } = new List<TClass>();
        public Pagination Pagination { get; set; } = new();
    }

    public class Pagination
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? Total { get; set; }
        public string? ContinuationSince { get; set; }
    }
}
