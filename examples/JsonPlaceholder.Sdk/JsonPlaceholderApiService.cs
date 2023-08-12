using fbognini.Sdk;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Logging;

namespace JsonPlaceholder.Sdk
{
    public interface IJsonPlaceholderApiService
    {
        Task<List<Post>> GetPosts();
    }

    internal class JsonPlaceholderApiService : BaseApiService, IJsonPlaceholderApiService
    {
        public JsonPlaceholderApiService(HttpClient client, ILogger<JsonPlaceholderApiService> logger)
            : base(client, logger, null)
        {
            client.BaseAddress = new Uri($"https://jsonplaceholder.typicode.com/");
        }

        public Task<List<Post>> GetPosts()
        {
            return GetApi<List<Post>>("posts");
        }
    }
}
