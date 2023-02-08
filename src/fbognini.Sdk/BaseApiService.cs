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
    public abstract class BaseApiService
    {
        protected readonly HttpClient client;
        private readonly ILogger<BaseApiService> logger;
        protected ISdkCurrentUserService? currentUserService;

        private readonly IHttpErrorHandler? httpErrorHandler;
        private readonly JsonSerializerOptions? options;

        protected AsyncRetryPolicy<HttpResponseMessage> AuthenticationEnsuringPolicy => Policy
                .HandleResult<HttpResponseMessage>(r =>
                {
                    return r.StatusCode == HttpStatusCode.Unauthorized;
                })
                .RetryAsync(
                    retryCount: 1,
                    onRetryAsync: async (outcome, retryNumber, context) =>
                    {
                        await ReloadAccessToken();
                    }
                );

        public BaseApiService(HttpClient client, ILogger<BaseApiService> logger, IHttpErrorHandler? httpErrorHandler = null, ISdkCurrentUserService? currentUserService = null, JsonSerializerOptions? options = null)
        {
            this.client = client;
            this.logger = logger;
            this.httpErrorHandler = httpErrorHandler;
            this.currentUserService = currentUserService;
            this.options = options;
        }

        protected async Task<T> GetApi<T>(string url)
        {
            return await ProcessApi<T>(()
                => client.GetAsync(url));
        }

        protected async Task<T> DeleteApi<T>(string url)
        {
            return await ProcessApi<T>(()
                => client.DeleteAsync(url));
        }

        protected async Task<T> PostApi<T>(string url, HttpContent? content)
        {
            return await ProcessApi<T>(()
                => client.PostAsync(url, content));
        }

        protected async Task<T> PostApi<T, TRequest>(string url, TRequest request)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApi<T>(url, content);
        }

        protected async Task<T> PutApi<T, TRequest>(string url, TRequest request)
        {
            return await ProcessApi<T>(()
                => client.PutAsJsonAsync(url, request, options));
        }

        protected virtual async Task SetAccessToken()
        {
            client.DefaultRequestHeaders.Authorization
                 = new AuthenticationHeaderValue("Bearer", await currentUserService!.GetAccessToken());
        }

        protected virtual async Task ReloadAccessToken()
        {
            await currentUserService!.ReloadAccessToken();
            await SetAccessToken();
        }

        protected virtual async Task<T> ProcessApi<T>(Func<Task<HttpResponseMessage>> action)
        {
            var _options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var baseAddress = client.BaseAddress?.ToString() ?? string.Empty;
            var sdk = this.GetType().Namespace!;

            var propertys = new Dictionary<string, object>()
            {
                ["IsSdk"] = true,
                ["Sdk"] = sdk,
                ["BaseAddress"] = baseAddress
            };

            string method = action.Method.Name[1..action.Method.Name.IndexOf("Api>")].ToUpper();
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
                    message = await AuthenticationEnsuringPolicy.ExecuteAsync(() => action());
                }
                else
                {
                    message = await action();
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
                var response = (await message.Content.ReadFromJsonAsync<T>(options))!;

                propertys.Add("Response", JsonSerializer.Serialize(response, _options));

                using (logger.BeginScope(propertys))
                {
                    logger.LogInformation("{Sdk} {Method} {Uri} responded {StatusCode}", sdk, method, uri, statusCode);
                }

                return response;
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

                return headers.SelectMany(x =>  x.Value, (header, value) => new KeyValuePair<string, string>(header.Key, value));
            }
        }

        #region ApiResult
        protected async Task<ApiResult<T>> GetApiResult<T>(string url)
             where T : class
        {
            return await ProcessApiResult<T>(()
                => client.GetAsync(url));
        }

        protected async Task<ApiResult<T>> DeleteApiResult<T>(string url)
            where T : class
        {
            return await ProcessApiResult<T>(()
                => client.DeleteAsync(url));
        }

        protected async Task<ApiResult<T>> PostApiResult<T>(string url)
           where T : class
        {
            return await ProcessApiResult<T>(()
                => client.PostAsync(url, null));
        }

        protected async Task<ApiResult<T>> PostApiResult<T, TRequest>(string url, TRequest request)
            where T : class
        {
            return await ProcessApiResult<T>(()
                => client.PostAsJsonAsync(url, request, options));
        }

        protected async Task<ApiResult<T>> PutApiResult<T, TRequest>(string url, TRequest request)
           where T : class
        {
            return await ProcessApiResult<T>(()
                => client.PutAsJsonAsync(url, request, options));
        }

        protected virtual async Task<ApiResult<T>> ProcessApiResult<T>(Func<Task<HttpResponseMessage>> action)
           where T : class
        {
            var response = await action();
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                    Response = await response.Content.ReadFromJsonAsync<T>(options)
                };
            }

            if (httpErrorHandler != null)
            {
                await httpErrorHandler.HandleResponse(response);
            }

            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Message = await response.Content.ReadAsStringAsync()
            };
        }

        #endregion
    }
}