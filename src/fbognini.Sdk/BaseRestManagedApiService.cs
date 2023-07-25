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
        protected async Task SendApiResult(HttpRequestMessage message)
        {
            await ProcessApiResult(message);
        }

        #region GET

        protected async Task<ManagedApiResult> GetApiResult(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult(BaseApiService.BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        protected async Task<ManagedApiResult<T>> GetApiResult<T>(string url, RequestOptions? requestOptions = null)
             where T : class
        {
            return await ProcessApiResult<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Get, url, requestOptions));
        }

        #endregion

        #region DELETE

        protected async Task<ManagedApiResult> DeleteApiResult(string url, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult(BaseApiService.BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        protected async Task<ManagedApiResult<T>> DeleteApiResult<T>(string url, RequestOptions? requestOptions = null)
            where T : class
        {
            return await ProcessApiResult<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Delete, url, requestOptions));
        }

        #endregion

        #region POST

        protected async Task<ManagedApiResult> PostApiResult(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult(BaseApiService.BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task<ManagedApiResult<T>> PostApiResult<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Post, url, content, requestOptions));
        }

        protected async Task<ManagedApiResult> PostApiResult<TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApiResult(url, content as HttpContent, requestOptions);
        }

        protected async Task<ManagedApiResult<T>> PostApiResult<T, TRequest>(string url, TRequest request, RequestOptions? requestOptions = null)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApiResult<T>(url, content as HttpContent, requestOptions);
        }

        #endregion

        #region PUT

        protected async Task<ManagedApiResult> PutApiResult(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult(BaseApiService.BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task<ManagedApiResult<T>> PutApiResult<T>(string url, HttpContent? content = null, RequestOptions? requestOptions = null)
        {
            return await ProcessApiResult<T>(BaseApiService.BuildHttpRequestMessage(HttpMethod.Put, url, content, requestOptions));
        }

        protected async Task<ManagedApiResult> PutApiResult<TRequest>(string url, TRequest request)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PutApiResult(url, content as HttpContent);
        }

        protected async Task<ManagedApiResult<T>> PutApiResult<T, TRequest>(string url, TRequest request)
        {
            // client.PutAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PutApiResult<T>(url, content as HttpContent);
        }

        #endregion

        private async Task<ManagedApiResult> ProcessApiResult(HttpRequestMessage message)
        {
            var response = await SendMessage(message);
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
                Raw = await response.Content.ReadAsStringAsync()
            };
        }

        private async Task<ManagedApiResult<T>> ProcessApiResult<T>(HttpRequestMessage message)
        {
            var response = await SendMessage(message);
            if (response.IsSuccessStatusCode)
            {
                return new ManagedApiResult<T>
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                    Response = (await response.Content.ReadFromJsonAsync<T>(options))!
                };
            }

            return new ManagedApiResult<T>
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Raw = await response.Content.ReadAsStringAsync()
            };
        }
    }
}