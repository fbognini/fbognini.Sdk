using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Extensions
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddHttpContextAuthenticationPolicy<TCurrentUserService>(this IHttpClientBuilder httpClientBuilder, Func<HttpResponseMessage, bool>? handle = null)
            where TCurrentUserService : ISdkCurrentUserService
        {
            httpClientBuilder.Services.AddHttpContextAccessor();

            httpClientBuilder.AddAuthenticationPolicy(
                sp =>
                {
                    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
                    if (httpContext is not null)
                    {
                        return httpContext.RequestServices.GetService<TCurrentUserService>();
                    }

                    return sp.GetService<TCurrentUserService>();
                },
                handle);

            return httpClientBuilder;
        }
    }
}
