﻿using fbognini.Sdk.Exceptions;
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
        public class LoggingProperys
        {
            public static bool IsSdk => true;
            public string Sdk { get; init; } = string.Empty;
            public string BaseAddress { get; init; } = string.Empty;
            public string Method { get; init; } = string.Empty;
            public string RequestUrl { get; init; } = string.Empty;
            public string Uri => $"{BaseAddress}{RequestUrl}";
            public string? RawRequest { get; set; }
            public bool? IsSuccessStatusCode { get; set; }
            public int? StatusCode { get; set; }
            public string? RawResponse { get; set; }
            public IEnumerable<KeyValuePair<string, string>>? ResponseHeaders { get; set; }
            public double? ElapsedMilliseconds { get; set; }

            public Dictionary<string, object?> ToLoggingDictionary() => new()
            {
                ["IsSdk"] = true,
                [nameof(Sdk)] = Sdk,
                [nameof(BaseAddress)] = BaseAddress,
                [nameof(Method)] = Method,
                [nameof(RequestUrl)] = RequestUrl,
                [nameof(Uri)] = Uri,
                [nameof(RawRequest)] = RawRequest,
                [nameof(IsSuccessStatusCode)] = IsSuccessStatusCode,
                [nameof(StatusCode)] = StatusCode,
                [nameof(RawResponse)] = RawResponse,
                [nameof(ResponseHeaders)] = ResponseHeaders,
                [nameof(ElapsedMilliseconds)] = ElapsedMilliseconds,
            };
        }

        private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage httpRequestMessage)
        {
            ArgumentNullException.ThrowIfNull(httpRequestMessage, nameof(httpRequestMessage));

            var loggingPropertys = new LoggingProperys
            {
                Sdk = this.GetType().Namespace!,
                BaseAddress = client.BaseAddress?.ToString() ?? string.Empty,
                Method = httpRequestMessage.Method.Method,
                RequestUrl = httpRequestMessage.RequestUri!.OriginalString,
                RawRequest = await GetRawRequest(httpRequestMessage.Content)
            };

            try
            {
                LogRequest(loggingPropertys);

                var stopwatch = new Stopwatch();

                HttpResponseMessage message;
                if (currentUserService != null)
                {
                    if (await currentUserService.IsAuthenticated())
                    {
                        await SetAuthorization();
                    }
                    else
                    {
                        await ResetAuthorization();
                    }

                    stopwatch.Start();

                    message = await AuthenticationEnsuringPolicy.ExecuteAsync(() => ExecuteAction(() => client.SendAsync(httpRequestMessage)));
                }
                else
                {
                    stopwatch.Start();

                    message = await ExecuteAction(() => client.SendAsync(httpRequestMessage));
                }

                stopwatch.Stop();

                loggingPropertys.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                loggingPropertys.IsSuccessStatusCode = message.IsSuccessStatusCode;
                loggingPropertys.StatusCode = (int)message.StatusCode;

                loggingPropertys.RawResponse = await message.Content.ReadAsStringAsync();
                loggingPropertys.ResponseHeaders = GetResponseHeaders(message.Headers).ToList();

                if (httpErrorHandler != null)
                {
                    await httpErrorHandler.HandleResponse(message);
                }

                LogResponse(loggingPropertys);

                return message;
            }
            catch (Exception ex)
            {
                LogException(loggingPropertys, ex);

                throw;
            }

            IEnumerable<KeyValuePair<string, string>> GetResponseHeaders(HttpResponseHeaders httpResponseHeaders)
            {
                var baseHeaders = httpResponseHeaders.GetType().GetProperties().Select(x => x.Name!).ToList();
                baseHeaders.Add("Set-Cookie");
                baseHeaders.Add("Cache-Control");
                baseHeaders.Add("X-AspNet-Version");
                baseHeaders.Add("X-Powered-By");
                baseHeaders.Add("Strict-Transport-Security");
                baseHeaders.Add("X-Content-Type-Options");
                baseHeaders.Add("X-Frame-Options");
                baseHeaders.Add("X-XSS-Protection");
                baseHeaders.Add("Transfer-Encoding");
                var headers = httpResponseHeaders
                    .Where(x => baseHeaders.Contains(x.Key) == false && x.Key.StartsWith("Access-Control") == false).ToList();

                return headers.SelectMany(x => x.Value, (header, value) => new KeyValuePair<string, string>(header.Key, value));
            }
        }
        
        private async Task<string?> GetRawRequest(HttpContent? content)
        {
            if (content == null)
            {
                return null;
            }

            if (content is JsonContent json)
            {
                return JsonSerializer.Serialize(json.Value, options);
            }

            if (content is ByteArrayContent byteArray)
            {
                return await byteArray.ReadAsStringAsync();
            }

            return null;
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
                    message.Headers.Add(header.Key, header.Value);
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