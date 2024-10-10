using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace JsonPlaceholder.Sdk
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonPlaceholderApiService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IJsonPlaceholderApiService, JsonPlaceholderApiService>()
                .AddAuthenticationPolicy<ISdkCurrentUserService>();

            return services;
        }
    }
}
