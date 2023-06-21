using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk
{
    public class ApiResult
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? Message { get; set; }

        public IDictionary<string, string[]> Validations { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    public class ApiResult<TResponse> : ApiResult
    {
        public TResponse? Response { get; set; }
    }

    public class ApiResult<TPagination, TResponse> : ApiResult 
        where TPagination : PaginationResponse<TResponse>
    {
        public List<TResponse> Response { get; set; }
        public Pagination Pagination { get; set; }
        public Links Links { get; set; }
    }


    public class PaginationResponse<TClass>
    {
        public PaginationResponse()
        {
            Pagination = new Pagination();
        }
        public List<TClass> Response { get; set; }
        public Pagination Pagination { get; set; }

        public static implicit operator PaginationResponse<TClass>(ApiResult<PaginationResponse<TClass>, TClass> data)
        {
            return new PaginationResponse<TClass>()
            {
                Response = data.Response,
                Pagination = data.Pagination
            };
        }
    }

    public class Pagination
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? Total { get; set; }
        public string ContinuationSince { get; set; }
    }

    public class Links
    {
        public string Next { get; set; }
        public string Prev { get; set; }
    }
}
