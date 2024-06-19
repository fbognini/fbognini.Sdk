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

        protected async Task<HttpResponseMessage> SendApi(HttpRequestMessage message)
        {
            return await ProcessApi(message);
        }

        #region GET

        protected async Task<HttpResponseMessage> GetApi(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        protected async Task<T> GetApi<T>(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        #endregion

        #region DELETE

        protected async Task<HttpResponseMessage> DeleteApi(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        protected async Task<T> DeleteApi<T>(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        #endregion

        #region POST


        protected async Task<HttpResponseMessage> PostApi(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task<T> PostApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task<HttpResponseMessage> PostApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = GetStringContent(request, requestOptions);
            return await PostApi(url, content as HttpContent, requestOptions);
        }

        protected async Task<T> PostApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = GetStringContent(request, requestOptions);
            return await PostApi<T>(url, content as HttpContent, requestOptions);
        }

        #endregion

        #region PUT

        protected async Task<HttpResponseMessage> PutApi(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task<T> PutApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task<HttpResponseMessage> PutApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = GetStringContent(request, requestOptions);
            return await PutApi(url, content as HttpContent, requestOptions);
        }

        protected async Task<T> PutApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = GetStringContent(request, requestOptions);
            return await PutApi<T>(url, content as HttpContent, requestOptions);
        }

        #endregion

        private async Task<HttpResponseMessage> ProcessApi(HttpRequestMessage message)
        {
            return await SendMessage(message);
        }

        private async Task<T> ProcessApi<T>(HttpRequestMessage message)
        {
            var response = await ProcessApi(message);
            return (await response.Content.ReadFromJsonAsync<T>(options))!;
        }

        private HttpContent GetStringContent<TRequest>(TRequest request, RequestOptions? requestOptions = null)
        {
            if (requestOptions != null && requestOptions.Encoding != null)
            {
                return new StringContent(JsonSerializer.Serialize(request, options), requestOptions.Encoding, "application/json");
            }

            return new StringContentWithoutCharset(JsonSerializer.Serialize(request, options), "application/json");
        }
    }
}