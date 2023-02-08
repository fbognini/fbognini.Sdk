using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Handlers
{
    public class ThrowApiResultIfNotSuccessHandler : IHttpErrorHandler
    {
        public async Task HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode == false)
            {
                throw new ApiException(response.RequestMessage.RequestUri.ToString(), await response.Content.ReadFromJsonAsync<ApiResult>());
            }
        }
    }

    public class ThrowIfNotSuccessHandler : IHttpErrorHandler
    {
        public async Task HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode == false)
            {
                throw new ApiException(response.RequestMessage.RequestUri.ToString(), (int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
