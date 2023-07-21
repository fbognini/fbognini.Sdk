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
    public class ThrowIfNotSuccessHandler : IHttpErrorHandler
    {
        public async Task HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode == false)
            {
                throw await ApiException.FromHttpResponseMessage(response);
            }
        }
    }
}
