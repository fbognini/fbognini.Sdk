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

        public const string SdkOptionName = "internal_25aI6plz";
        public const string BaseAddressOptionName = "internal_iE97RqE3";

        private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage httpRequestMessage)
        {
            ArgumentNullException.ThrowIfNull(httpRequestMessage, nameof(httpRequestMessage));

            httpRequestMessage.Options.TryAdd(SdkOptionName, this.GetType().Namespace);
            httpRequestMessage.Options.TryAdd(BaseAddressOptionName, client.BaseAddress?.ToString() ?? string.Empty);

            await SetAuthorization(httpRequestMessage);

            var message = await client.SendAsync(httpRequestMessage);

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
                foreach (var header in requestOptions.Headers)
                {
                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (requestOptions.Options != null)
            {
                AddOptions(message, requestOptions.Options);    
            }

            return message;
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