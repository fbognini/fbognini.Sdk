using fbognini.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Exceptions
{
    public class ApiException : Exception
    {
        public string? Url { get; init; }
        public string? Response { get; init; }
        public int StatusCode { get; init; }

        public static async Task<ApiException> FromHttpResponseMessage(HttpResponseMessage response)
        {
            return await FromHttpResponseMessage<ApiException>(response);
        }

        public static async Task<T> FromHttpResponseMessage<T>(HttpResponseMessage response)
            where T : ApiException, new()
        {
            var exception = new T()
            {
                Url = response.RequestMessage!.RequestUri!.ToString(),
                StatusCode = (int)response.StatusCode,
                Response = await response.Content.ReadAsStringAsync()
            };

            return exception;
        }
    }
}
