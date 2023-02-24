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
                        await ReloadAuthorization();
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

        protected async Task<T> PostApi<T>(string url)
        {
            return await ProcessApi<T>(()
                => client.PostAsync(url, null));
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

        protected virtual async Task SetAuthorization()
        {
            client.DefaultRequestHeaders.Authorization
                 = new AuthenticationHeaderValue("Bearer", await currentUserService!.GetAccessToken());
        }

        protected virtual Task ResetAuthorization()
        {
            client.DefaultRequestHeaders.Authorization = null;
            return Task.CompletedTask;
        }

        protected virtual async Task ReloadAuthorization()
        {
            await currentUserService!.ReloadAccessToken();
            await SetAuthorization();
        }

        protected virtual async Task<HttpResponseMessage> ExecuteAction(Func<Task<HttpResponseMessage>> action)
        {
            return await action();
        }

        protected virtual async Task<T> ProcessApi<T>(Func<Task<HttpResponseMessage>> action)
        {
            var response = await SendAction(action);
            return (await response.Content.ReadFromJsonAsync<T>(options))!;
        }
    }
}