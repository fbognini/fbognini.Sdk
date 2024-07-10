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
        protected Task SendApiResult(HttpRequestMessage message, CancellationToken cancellationToken = default) => SendApiResultAsync(message, cancellationToken);
        protected async Task SendApiResultAsync(HttpRequestMessage message, CancellationToken cancellationToken = default)
        {
            await ProcessApiResult(message, cancellationToken);
        }

        #region GET

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult> GetApiResult(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => GetApiResultAsync(url, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult> GetApiResultAsync(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions), cancellationToken);
        }

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult<T>> GetApiResult<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => GetApiResultAsync<T>(url, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult<T>> GetApiResultAsync<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions), cancellationToken);
        }

        #endregion

        #region DELETE

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult> DeleteApiResult(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => DeleteApiResultAsync(url, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult> DeleteApiResultAsync(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions), cancellationToken);
        }

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult<T>> DeleteApiResult<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => DeleteApiResultAsync<T>(url, requestOptions, cancellationToken);    
        protected async Task<ManagedApiResult<T>> DeleteApiResultAsync<T>(string url, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions), cancellationToken);
        }

        #endregion

        #region POST

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult<T>> PostApiResult<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiResultAsync<T>(url, content, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult<T>> PostApiResultAsync<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult> PostApiResult<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiResultAsync<TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult> PostApiResultAsync<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApiResult(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use SendApiAsync()")]
        protected Task<ManagedApiResult<T>> PostApiResult<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PostApiResultAsync<T, TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult<T>> PostApiResultAsync<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions), cancellationToken);
        }

        #endregion

        #region PUT

        [Obsolete("Please use PutApiResultAsync<T>()")]
        protected Task<ManagedApiResult<T>> PutApiResult<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PutApiResultAsync<T>(url, content, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult<T>> PutApiResultAsync<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use PutApiResultAsync<TRequest>()")]
        protected Task<ManagedApiResult> PutApiResult<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) => PutApiResultAsync<TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult> PutApiResultAsync<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApiResult(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        [Obsolete("Please use  PutApiResultAsync<T, TRequest>()")]
        protected Task<ManagedApiResult<T>> PutApiResult<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>  PutApiResultAsync<T, TRequest>(url, request, requestOptions, cancellationToken);
        protected async Task<ManagedApiResult<T>> PutApiResultAsync<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var content = GetHttpContentContent(request, requestOptions);
            return await ProcessApiResult<T>(BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions), cancellationToken);
        }

        #endregion

        private async Task<ManagedApiResult> ProcessApiResult(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var response = await SendMessage(message, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ManagedApiResult
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                };
            }

            return new ManagedApiResult
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Raw = await response.Content.ReadAsStringAsync(cancellationToken)
            };
        }

        private async Task<ManagedApiResult<T>> ProcessApiResult<T>(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var response = await SendMessage(message, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<T>(options, cancellationToken: cancellationToken);
                return new ManagedApiResult<T>
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                    Response = json!
                };
            }

            return new ManagedApiResult<T>
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Raw = await response.Content.ReadAsStringAsync(cancellationToken)
            };
        }
    }
}