using fbognini.Sdk.Handlers;
using fbognini.Sdk.Interfaces;
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
        public static IHttpClientBuilder AddAuthenticationPolicy(this IHttpClientBuilder httpClientBuilder)
        {
            httpClientBuilder.AddPolicyHandler((sp, request) =>
            {
                var currentUserService = sp.GetService<ISdkCurrentUserService>();
                if (currentUserService == null)
                {
                    return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                }

                return Policy
                    .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
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
    }
}
