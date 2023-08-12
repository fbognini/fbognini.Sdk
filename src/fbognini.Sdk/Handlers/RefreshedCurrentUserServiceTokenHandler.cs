using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Handlers
{
    internal class CurrentUserServiceToken
    {
        public string Scheme { get; set; }
        public string AccessToken { get; set; }
    }

    internal class RefreshedCurrentUserServiceTokenHandler : DelegatingHandler
    {
        public const string TokenRetrieval = nameof(TokenRetrieval);
        public const string TokenKey = nameof(TokenKey);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = request.GetPolicyExecutionContext();
            if (context.Count > 0 && context.TryGetValue(TokenKey, out var tokenAsObject) && tokenAsObject is CurrentUserServiceToken token)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.AccessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
