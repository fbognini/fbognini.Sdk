using fbognini.Sdk.Exceptions;

namespace fbognini.Sdk.Handlers
{
    public class ThrowIfNotSuccessHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            throw await ApiException.FromHttpResponseMessage(response);
        }
    }
}
