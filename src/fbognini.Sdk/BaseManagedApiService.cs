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
        protected async Task<ApiResult> GetApiResult(string url)
        {
            return await ProcessApiResult(()
                => client.GetAsync(url));
        }

        protected async Task<ApiResult<T>> GetApiResult<T>(string url)
             where T : class
        {
            return await ProcessApiResult<T>(()
                => client.GetAsync(url));
        }


        protected async Task<ApiResult> DeleteApiResult(string url)
        {
            return await ProcessApiResult(()
                => client.DeleteAsync(url));
        }

        protected async Task<ApiResult<T>> DeleteApiResult<T>(string url)
            where T : class
        {
            return await ProcessApiResult<T>(()
                => client.DeleteAsync(url));
        }

        protected async Task<ApiResult> DeleteApiResult<TRequest>(string url, TRequest request)
        {
            return await ProcessApiResult(()
                => client.DeleteAsJsonAsync(url, request));
        }

        protected async Task<ApiResult<T>> DeleteApiResult<T, TRequest>(string url, TRequest request)
        {
            return await ProcessApiResult<T>(()
                => client.DeleteAsJsonAsync(url, request));
        }

        protected async Task<ApiResult> PostApiResult(string url, HttpContent? content = null)
        {
            return await ProcessApiResult(()
                => client.PostAsync(url, content));
        }

        protected async Task<ApiResult<T>> PostApiResult<T>(string url, HttpContent? content = null)
        {
            return await ProcessApiResult<T>(()
                => client.PostAsync(url, content));
        }

        protected async Task<ApiResult> PostApiResult<TRequest>(string url, TRequest request)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApiResult(url, content as HttpContent);
        }

        protected async Task<ApiResult<T>> PostApiResult<T, TRequest>(string url, TRequest request)
        {
            // client.PostAsJsonAsync don't use Header Content-type application/json
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            return await PostApiResult<T>(url, content as HttpContent);
        }

        protected async Task<ApiResult<T>> PutApiResult<T, TRequest>(string url, TRequest request)
        {
            return await ProcessApiResult<T>(()
                => client.PutAsJsonAsync(url, request));
        }

        protected virtual async Task<ApiResult> ProcessApiResult(Func<Task<HttpResponseMessage>> action)
        {
            var response = await SendAction(action);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                };
            }

            return new ApiResult
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Message = await response.Content.ReadAsStringAsync()
            };
        }

        protected virtual async Task<ApiResult<T>> ProcessApiResult<T>(Func<Task<HttpResponseMessage>> action)
        {
            var response = await SendAction(action);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    IsSuccess = true,
                    StatusCode = response.StatusCode,
                    Response = (await response.Content.ReadFromJsonAsync<T>(options))!
                };
            }

            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = response.StatusCode,
                Message = await response.Content.ReadAsStringAsync()
            };
        }
    }
}