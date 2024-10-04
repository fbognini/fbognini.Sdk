using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Handlers;
using fbognini.Sdk.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace fbognini.Sdk.Extensions
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddAuthenticationPolicy<TCurrentUserService>(this IHttpClientBuilder httpClientBuilder, Func<HttpResponseMessage, bool>? handle = null)
            where TCurrentUserService : ISdkCurrentUserService
        {
            httpClientBuilder.Services.AddHttpContextAccessor();

            handle ??= r =>
            {
                var isUnauthorized = r.StatusCode == HttpStatusCode.Unauthorized;
                return isUnauthorized;
            };

            httpClientBuilder.AddPolicyHandler((sp, request) =>
            {
                var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
                var currentUserService = httpContext is not null ? httpContext.RequestServices.GetService<TCurrentUserService>() : sp.GetService<TCurrentUserService>();
                if (currentUserService == null)
                {
                    return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                }

                return Policy
                    .Handle<ApiException>((ex) => handle(ex.HttpResponseMessage))
                    .OrResult<HttpResponseMessage>(handle)
                    .RetryAsync(1, async (_, __, context) =>
                    {
                        var accessToken = await currentUserService.ReloadAccessToken();
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            request.SetPolicyExecutionContext(new Context(RefreshedCurrentUserServiceTokenHandler.TokenRetrieval,
                                new Dictionary<string, object>
                                {
                                    [RefreshedCurrentUserServiceTokenHandler.TokenKey] = new CurrentUserServiceToken
                                    {
                                        Scheme = currentUserService.Schema,
                                        AccessToken = accessToken,
                                    }
                                }));
                        }
                    });
            });

            httpClientBuilder.Services.AddTransient<RefreshedCurrentUserServiceTokenHandler>();
            httpClientBuilder.AddHttpMessageHandler<RefreshedCurrentUserServiceTokenHandler>();

            return httpClientBuilder;
        }

        public static IHttpClientBuilder ThrowApiExceptionIfNotSuccess(this IHttpClientBuilder builder)
        {
            builder.Services.AddTransient<ThrowIfNotSuccessHandler>();
            builder.AddHttpMessageHandler<ThrowIfNotSuccessHandler>();

            return builder;
        }


        /// <summary>
        /// Add logging handler. It should be specified as last handler.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddLogging(this IHttpClientBuilder builder)
        {
            builder.Services.AddTransient<LoggingHandler>();
            builder.AddHttpMessageHandler<LoggingHandler>();

            return builder;
        }
    }
}
