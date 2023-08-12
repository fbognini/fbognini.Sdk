using fbognini.Sdk.Exceptions;
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
        protected readonly HttpClient client;
        private readonly ILogger<BaseApiService> logger;
        protected ISdkCurrentUserService? currentUserService;

        private readonly JsonSerializerOptions? options;

        public BaseApiService(HttpClient client, ILogger<BaseApiService> logger, ISdkCurrentUserService? currentUserService = null, JsonSerializerOptions? options = null)
        {
            this.client = client;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.options = options;
        }

        /*
         * Please don't use Polly in this method
         * https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory
         */
        protected virtual async Task<HttpResponseMessage> ExecuteAction(Func<Task<HttpResponseMessage>> action)
        {
            return await action();
        }

        protected virtual void LogRequest(LoggingProperys loggingPropertys)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                logger.LogInformation("{Sdk} requesting {Method} {Uri}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri);
            }
        }

        protected virtual void LogResponse(LoggingProperys loggingPropertys)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                logger.LogInformation("{Sdk} {Method} {Uri} responded {StatusCode} in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri, loggingPropertys.StatusCode, loggingPropertys.ElapsedMilliseconds);
            }
        }

        protected virtual void LogException(LoggingProperys loggingPropertys, Exception exception)
        {
            using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
            {
                if (exception is ApiException apiException)
                {
                    logger.LogWarning("{Sdk} {Method} {Uri} responded {StatusCode} in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri, apiException.StatusCode, loggingPropertys.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogError(exception, "{Sdk} failed to ask for {Method} {Uri}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Uri);
                }
            }
        }
    }
}