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
            services.AddJsonPlaceholderApiService<FakeHttpErrorHandler>(configuration);

            return services;
        }

        public static IServiceCollection AddJsonPlaceholderApiService<THttpErrorHandler>(this IServiceCollection services, IConfiguration configuration)
            where THttpErrorHandler : class, IHttpErrorHandler, new()
        {
            services.AddScoped<IHttpErrorHandler, THttpErrorHandler>();

            services.AddHttpClient<IJsonPlaceholderApiService, JsonPlaceholderApiService>();

            return services;
        }
    }
}
