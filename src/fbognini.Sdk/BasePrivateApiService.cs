using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using fbognini.Sdk.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace fbognini.Sdk
{
    public abstract partial class BaseApiService
    {
        public HttpRequestOptions DefaultRequestOptions { get; } = new();

        public const string SdkOptionName = "__fbognini_25aI6plz__";
        public const string BaseAddressOptionName = "__fbognini_iE97RqE3__";

        private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpRequestMessage, nameof(httpRequestMessage));

            httpRequestMessage.Options.TryAdd(SdkOptionName, this.GetType().Namespace);
            httpRequestMessage.Options.TryAdd(BaseAddressOptionName, client.BaseAddress?.ToString() ?? string.Empty);

            await SetAuthorization(httpRequestMessage);

            var message = await client.SendAsync(httpRequestMessage, cancellationToken);

            return message;
        }
        
        private HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, RequestOptions? requestOptions)
        {
            return BuildHttpRequestMessage(method, url, null, requestOptions);
        }

        private HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, HttpContent? content, RequestOptions? requestOptions)
        {
            var message = new HttpRequestMessage(method, url)
            {
                Content = content
            };


            AddOptions(message, DefaultRequestOptions);

            if (requestOptions == null)
            {
                return message;
            }

            if (requestOptions.Headers != null)
            {
                requestOptions.Headers.CopyTo(message.Headers);
            }

            if (requestOptions.Options != null)
            {
                AddOptions(message, requestOptions.Options);    
            }

            return message;
        }

        private HttpContent? GetHttpContentContent<TRequest>(TRequest request, RequestOptions? requestOptions = null)
        {
            if (request is null)
            {
                return null;
            }

            if (request is HttpContent httpContent)
            {
                return httpContent;
            }

            // client.PostAsJsonAsync don't use Header Content-type application/json
            if (requestOptions != null && requestOptions.Encoding != null)
            {
                return new StringContent(JsonSerializer.Serialize(request, options), requestOptions.Encoding, "application/json");
            }

            return new StringContentWithoutCharset(JsonSerializer.Serialize(request, options), "application/json");
        }

        private static void AddOptions(HttpRequestMessage message, HttpRequestOptions options)
        {
            if (options is null)
            {
                return;
            }

            foreach (var option in options.Where(x => x.Value != null))
            {
                message.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value!);
            }
        }
    }
}