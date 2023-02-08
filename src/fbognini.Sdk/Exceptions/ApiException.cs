using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Exceptions
{
    public class ApiException : Exception
    {
        public string Url { get; }
        public string Response { get; }
        public int StatusCode { get; }
        public ApiResult Result { get; }

        public ApiException(string url, ApiResult result)
        {
            Url = url;
            StatusCode = (int)result.StatusCode;
            Result = result;
        }

        public ApiException(string url, int statusCode, string response)
        {
            Url = url;
            StatusCode = statusCode;
            Response = response;
        }
    }
}
