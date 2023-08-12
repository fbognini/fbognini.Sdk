using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Interfaces;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Handlers
{
    public class ThrowIfNotSuccessHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode == false)
            {
                throw await ApiException.FromHttpResponseMessage(response);
            }

            return response;
        }
    }
}
