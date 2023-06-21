using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using static fbognini.Sdk.BaseApiService;

namespace fbognini.Sdk
{
    public abstract partial class BaseApiService
    {
        public class LoggingProperys
        {
            public bool IsSdk => true;
            public string Sdk { get; set; }
            public string BaseAddress { get; set; }
            public string Method { get; set; }
            public string RequestUrl { get; set; }
            public string Uri => $"{BaseAddress}{RequestUrl}";
            public string RawRequest { get; set; }
            public bool IsSuccessStatusCode { get; set; }
            public int StatusCode { get; set; }
            public string RawResponse { get; set; }
            public IEnumerable<KeyValuePair<string, string>> ResponseHeaders { get; set; }

            public Dictionary<string, object> ToLoggingDictionary() => new Dictionary<string, object>()
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
            };
        }


        protected async Task<HttpResponseMessage> SendAction(Func<Task<HttpResponseMessage>> action)
        {
            var loggingPropertys = new LoggingProperys()
            {
                Sdk = this.GetType().Namespace!,
                BaseAddress = client.BaseAddress?.ToString() ?? string.Empty,
            };

            var index = action.Method.Name.IndexOf("ApiResult>");
            if (index == -1)
            {
                index = action.Method.Name.IndexOf("Api>");
            }

            loggingPropertys.Method = action.Method.Name[1..index].ToUpper();

            var type = action.Target!.GetType();

            loggingPropertys.RequestUrl = type.GetField("url")!.GetValue(action.Target)!.ToString()!;

            var contentField = type.GetField("content");
            if (contentField != null)
            {
                var content = contentField.GetValue(action.Target)!;
                if (content is JsonContent json)
                {
                    loggingPropertys.RawRequest = JsonSerializer.Serialize(json.Value, options);
                }
                else if (content is ByteArrayContent byteArray)
                {
                    loggingPropertys.RawRequest = await byteArray.ReadAsStringAsync();
                }
            }

            try
            {
                LogRequest(loggingPropertys);

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

                    message = await AuthenticationEnsuringPolicy.ExecuteAsync(() => ExecuteAction(action));
                }
                else
                {
                    message = await ExecuteAction(action);
                }

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

        protected virtual void LogRequest(LoggingProperys loggingPropertys)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                logger.LogInformation("{Sdk} requesting {Method} {Uri}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri);
            }
        }

        protected virtual void LogResponse(LoggingProperys loggingPropertys)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                logger.LogInformation("{Sdk} {Method} {Uri} responded {StatusCode}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri, loggingPropertys.StatusCode);
            }
        }

        protected virtual void LogException(LoggingProperys loggingPropertys, Exception exception)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                if (exception is ApiException apiException)
                {
                    logger.LogWarning("{Sdk} {Method} {Uri} responded {StatusCode}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri, apiException.StatusCode);
                }
                else
                {
                    logger.LogError(exception, "{Sdk} failed to ask for {Method} {Uri}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri);
                }
            }
        }
    }
}