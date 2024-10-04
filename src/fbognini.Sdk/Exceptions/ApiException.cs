using fbognini.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Exceptions
{
    public class ApiException : Exception
    {
        [Obsolete("Please use Endpoint")]
        public string? Url => Endpoint;
        public string? Endpoint { get; init; }

        [Obsolete("Please use Content")]
        public string? Response => Content;
        public string? Content { get; init; }

        [Obsolete("Please use HttpStatusCode")]
        public int StatusCode => (int)HttpStatusCode;
        public HttpStatusCode HttpStatusCode { get; init; }

        public HttpResponseMessage HttpResponseMessage { get; init; } = default!;


        public static async Task<ApiException> FromHttpResponseMessage(HttpResponseMessage response)
        {
            return await FromHttpResponseMessage<ApiException>(response);
        }

        public static async Task<T> FromHttpResponseMessage<T>(HttpResponseMessage response)
            where T : ApiException, new()
        {
            var exception = new T()
            {
                Endpoint = response.RequestMessage!.RequestUri!.ToString(),
                Content = await response.Content.ReadAsStringAsync(),
                HttpStatusCode = response.StatusCode,
                HttpResponseMessage = response
            };

            return exception;
        }
    }
}
