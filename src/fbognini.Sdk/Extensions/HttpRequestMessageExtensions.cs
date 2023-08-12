using fbognini.Sdk.Handlers;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace fbognini.Sdk.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        internal static bool TryGetPolicyRefreshToken(this HttpRequestMessage request, out CurrentUserServiceToken token)
        {
            var context = request.GetPolicyExecutionContext();
            if (context != null && context.Count > 0 && context.TryGetValue(RefreshedCurrentUserServiceTokenHandler.TokenKey, out var tokenAsObject) && tokenAsObject is CurrentUserServiceToken)
            {
                token = (CurrentUserServiceToken)tokenAsObject;
                return true;
            }

            token = new CurrentUserServiceToken();
            return false;
        }

        public static bool HasPendingRefreshToken(this HttpRequestMessage request)
        {
            return TryGetPolicyRefreshToken(request, out _);
        }
    }
}
