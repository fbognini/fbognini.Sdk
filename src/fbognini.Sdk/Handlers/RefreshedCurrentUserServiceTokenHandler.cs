using fbognini.Sdk.Extensions;
using System.Net.Http.Headers;

namespace fbognini.Sdk.Handlers
{
    internal class CurrentUserServiceToken
    {
        public string Scheme { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }

    internal class RefreshedCurrentUserServiceTokenHandler : DelegatingHandler
    {
        public const string TokenRetrieval = nameof(TokenRetrieval);
        public const string TokenKey = nameof(TokenKey);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.TryGetPolicyRefreshToken(out var token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.AccessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
