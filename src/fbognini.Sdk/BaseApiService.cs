using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace fbognini.Sdk
{
    public abstract partial class BaseApiService
    {
        protected readonly HttpClient client;
        protected ISdkCurrentUserService? currentUserService;
        protected readonly JsonSerializerOptions? _jsonSerializerOptions;

        protected virtual LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

        public BaseApiService(HttpClient client, ISdkCurrentUserService? currentUserService = null, JsonSerializerOptions? options = null)
        {
            this.client = client;
            this.currentUserService = currentUserService;
            _jsonSerializerOptions = options;
        }

        protected virtual async Task SetAuthorization(HttpRequestMessage httpRequestMessage)
        {
            if (currentUserService != null && await currentUserService.IsAuthenticated())
            {
                var accessToken = await currentUserService.GetAccessToken();
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(currentUserService.Schema, accessToken);
            }
        }
    }
}