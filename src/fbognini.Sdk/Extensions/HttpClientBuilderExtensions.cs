using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Handlers;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Net;

namespace fbognini.Sdk.Extensions
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddAuthenticationPolicy<TCurrentUserService>(this IHttpClientBuilder httpClientBuilder, Func<HttpResponseMessage, bool>? isUnauthorizedPredicate = null)
            where TCurrentUserService : ISdkCurrentUserService
        {
            httpClientBuilder.AddAuthenticationPolicy(sp => sp.GetService<TCurrentUserService>(), isUnauthorizedPredicate);

            return httpClientBuilder;
        }

        public static IHttpClientBuilder AddAuthenticationPolicy<TCurrentUserService>(this IHttpClientBuilder httpClientBuilder, Func<IServiceProvider, TCurrentUserService?> currentUserServicePredicate, Func<HttpResponseMessage, bool>? isUnauthorizedPredicate = null)
            where TCurrentUserService : ISdkCurrentUserService
        {
            isUnauthorizedPredicate ??= r => r.StatusCode == HttpStatusCode.Unauthorized;

            httpClientBuilder.AddPolicyHandler((sp, request) =>
            {
                var currentUserService = currentUserServicePredicate(sp);
                if (currentUserService == null)
                {
                    return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                }

                return Policy
                    .Handle<ApiException>((ex) => isUnauthorizedPredicate(ex.HttpResponseMessage))
                    .OrResult<HttpResponseMessage>(isUnauthorizedPredicate)
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
