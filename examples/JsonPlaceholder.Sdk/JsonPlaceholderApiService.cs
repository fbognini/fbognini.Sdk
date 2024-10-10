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
        public JsonPlaceholderApiService(HttpClient client)
            : base(client, null)
        {
            client.BaseAddress = new Uri($"https://jsonplaceholder.typicode.com/");
        }

        public Task<List<Post>> GetPosts()
        {
            return GetApiAsync<List<Post>>("posts");
        }
    }
}
