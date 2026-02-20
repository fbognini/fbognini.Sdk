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

        [Obsolete("Please use SendApiAsync()")]
        protected Task<HttpResponseMessage> SendApi(HttpRequestMessage message, CancellationToken cancellationToken = default) => SendApiAsync(message, cancellationToken);
        protected async Task<HttpResponseMessage> SendApiAsync(HttpRequestMessage message, CancellationToken cancellationToken = default)
        {
            return await ProcessApi(message, cancellationToken);
        }

        #region GET

        [Obsolete("Please use GetApiAsync()")]
        protected Task<HttpResponseMessage> GetApi(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => GetApiAsync(url, requestOptions, cancellationToken);
        protected async Task<HttpResponseMessage> GetApiAsync(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions), cancellationToken);
        }

        [Obsolete("Please use GetApiAsync<T>()")]
        protected Task<T> GetApi<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => GetApiAsync<T>(url, requestOptions, cancellationToken);
        protected async Task<T> GetApiAsync<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions), cancellationToken);
        }

        #endregion

        #region DELETE

        [Obsolete("Please use DeleteApiAsync()")]
        protected Task<HttpResponseMessage> DeleteApi(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => DeleteApiAsync(url, requestOptions, cancellationToken);
        protected async Task<HttpResponseMessage> DeleteApiAsync(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions), cancellationToken);
        }

        [Obsolete("Please use DeleteApiAsync<T>()")]
        protected Task<T> DeleteApi<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => DeleteApiAsync<T>(url, requestOptions, cancellationToken);
        protected async Task<T> DeleteApiAsync<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions), cancellationToken);
        }

        #endregion

        #region POST

        protected async Task<HttpResponseMessage> PostApiAsync(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PostApiAsync<T>()")]
        protected Task<T> PostApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiAsync<T>(url, content, requestOptions, cancellationToken);
        protected async Task<T> PostApiAsync<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PostApiAsync<TRequest>()")]
        protected Task<HttpResponseMessage> PostApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiAsync<TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<HttpResponseMessage> PostApiAsync<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PostApiAsync<T, TRequest>()")]
        protected Task<T> PostApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiAsync<T, TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<T> PostApiAsync<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        #endregion

        #region PATCH

        protected async Task<T> PatchApiAsync<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Patch, url, content, requestOptions), cancellationToken);
        }

        protected async Task<HttpResponseMessage> PatchApiAsync<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Patch, url, content, requestOptions), cancellationToken);
        }

        protected async Task<T> PatchApiAsync<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Patch, url, content, requestOptions), cancellationToken);
        }

        #endregion

        #region PUT

        [Obsolete("Please use PutApiAsync<T>()")]
        protected Task<T> PutApi<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PutApiAsync<T>(url, content, requestOptions, cancellationToken);
        protected async Task<T> PutApiAsync<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PutApiAsync<TRequest>()")]
        protected Task<HttpResponseMessage> PutApi<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PutApiAsync<TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<HttpResponseMessage> PutApiAsync<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PutApiAsync<T, TRequest>()")]
        protected Task<T> PutApi<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PutApiAsync<T, TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<T> PutApiAsync<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApi<T>(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        #endregion

        private async Task<HttpResponseMessage> ProcessApi(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            return await SendMessage(message, cancellationToken);
        }

        private async Task<T> ProcessApi<T>(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var response = await ProcessApi(message, cancellationToken);
            var json = await response.Content.ReadFromJsonAsync<T>(_jsonSerializerOptions, cancellationToken: cancellationToken);
            return json!;
        }
    }
}