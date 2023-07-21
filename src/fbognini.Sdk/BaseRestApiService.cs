using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using fbognini.Sdk.Models;
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

        protected async Task SendApi(HttpRequestMessage message)
        {
            await ProcessApi(message);
        }

        #region GET

        protected async Task GetApi(string url, RequestOptions? requestOptions = null)
        {
            await ProcessApi(BaseApiService.BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        protected async Task<T> GetApi<T>(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        #endregion

        #region DELETE

        protected async Task DeleteApi(string url, RequestOptions? requestOptions = null)
        {
            await ProcessApi(BaseApiService.BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        protected async Task<T> DeleteApi<T>(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        #endregion

        #region POST

        protected async Task PostApi(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            await ProcessApi(BaseApiService.BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task<T> PostApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task PostApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            await PostApi(url, content as HttpContent);
        }

        protected async Task<T> PostApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApi<T>(url, content as HttpContent);
        }

        #endregion

        #region PUT

        protected async Task PutApi(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            await ProcessApi(BaseApiService.BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task<T> PutApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task PutApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            await PutApi(url, content as HttpContent);
        }

        protected async Task<T> PutApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PutApi<T>(url, content as HttpContent);
        }

        #endregion

        private async Task ProcessApi(HttpRequestMessage message)
        {
            await SendMessage(message);
        }

        private async Task<T> ProcessApi<T>(HttpRequestMessage message)
        {
            var response = await SendMessage(message);
            return (await response.Content.ReadFromJsonAsync<T>(options))!;
        }
    }
}