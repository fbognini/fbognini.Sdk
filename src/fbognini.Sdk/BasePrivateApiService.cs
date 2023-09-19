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
        public const string SdkOptionName = "Sdk";
        public const string BaseAddressOptionName = "BaseAddress";

        private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage httpRequestMessage)
        {
            ArgumentNullException.ThrowIfNull(httpRequestMessage, nameof(httpRequestMessage));

            httpRequestMessage.Options.TryAdd(SdkOptionName, this.GetType().Namespace);
            httpRequestMessage.Options.TryAdd(BaseAddressOptionName, client.BaseAddress?.ToString() ?? string.Empty);

            await SetAuthorization(httpRequestMessage);

            var message = await client.SendAsync(httpRequestMessage);

            return message;
        }
        
        private static HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, RequestOptions? requestOptions)
        {
            return BuildHttpRequestMessage(method, url, null, requestOptions);
        }

        private static HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, HttpContent? content, RequestOptions? requestOptions)
        {
            var message = new HttpRequestMessage(method, url)
            {
                Content = content
            };

            if (requestOptions == null)
            {
                return message;
            }

            if (requestOptions.Headers != null)
            {
                foreach (var header in requestOptions.Headers)
                {
                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (requestOptions.Options != null)
            {
                foreach (var option in requestOptions.Options.Where(x => x.Value != null))
                {
                    message.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value!);
                }
            }

            return message;
        }
    }
}