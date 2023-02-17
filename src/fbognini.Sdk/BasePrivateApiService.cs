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

namespace fbognini.Sdk
{
    public abstract partial class BaseApiService
    {
        protected async Task<HttpResponseMessage> SendAction(Func<Task<HttpResponseMessage>> action)
        {
            var baseAddress = client.BaseAddress?.ToString() ?? string.Empty;
            var sdk = this.GetType().Namespace!;

            var propertys = new Dictionary<string, object>()
            {
                ["IsSdk"] = true,
                ["Sdk"] = sdk,
                ["BaseAddress"] = baseAddress
            };

            var index = action.Method.Name.IndexOf("ApiResult>");
            if (index == -1)
            {
                index = action.Method.Name.IndexOf("Api>");
            }

            string method = action.Method.Name[1..index].ToUpper();
            propertys.Add("Method", method);

            var type = action.Target!.GetType();

            var url = type.GetField("url")!.GetValue(action.Target)!.ToString()!;
            var uri = $"{baseAddress}{url}";
            propertys.Add("RequestUrl", url);
            propertys.Add("Uri", uri);
            var contentField = type.GetField("content");
            if (contentField != null)
            {
                var content = contentField.GetValue(action.Target)!;
                if (content is JsonContent json)
                {
                    propertys.Add("Request", JsonSerializer.Serialize(json.Value, options));
                }
                else if (content is ByteArrayContent byteArray)
                {
                    propertys.Add("Request", await byteArray.ReadAsStringAsync());
                }
            }

            try
            {
                using (logger.BeginScope(propertys))
                {
                    logger.LogInformation("{Sdk} requesting {Method} {Uri}", sdk, method, uri);
                }

                HttpResponseMessage message;
                if (currentUserService != null)
                {
                    if (await currentUserService.IsAuthenticated())
                    {
                        await SetAccessToken();
                    }

                    message = await AuthenticationEnsuringPolicy.ExecuteAsync(() => ExecuteAction(action));
                }
                else
                {
                    message = await ExecuteAction(action);
                }

                int statusCode = (int)message.StatusCode;

                var headers = GetResponseHeaders(message.Headers).ToList();
                propertys.Add("ResponseHeaders", headers);

                propertys.Add("IsSuccessStatusCode", message.IsSuccessStatusCode);
                propertys.Add("StatusCode", statusCode);

                var rawResponse = await message.Content.ReadAsStringAsync();
                propertys.Add("RawResponse", rawResponse);

                if (httpErrorHandler != null)
                {
                    await httpErrorHandler.HandleResponse(message);
                }

                using (logger.BeginScope(propertys))
                {
                    logger.LogInformation("{Sdk} {Method} {Uri} responded {StatusCode}", sdk, method, uri, statusCode);
                }

                return message;
            }
            catch (Exception ex)
            {
                using (logger.BeginScope(propertys))
                {
                    logger.LogError(ex, "{Sdk} failed to ask for {Method} {Uri}", sdk, method, uri);
                }

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

    }
}