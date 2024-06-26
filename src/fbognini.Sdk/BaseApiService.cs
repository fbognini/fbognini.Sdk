﻿using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using fbognini.Sdk.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace fbognini.Sdk
{
    public abstract partial class BaseApiService
    {
        protected readonly HttpClient client;
        protected ISdkCurrentUserService? currentUserService;

        private readonly JsonSerializerOptions? options;

        public BaseApiService(HttpClient client, ISdkCurrentUserService? currentUserService = null, JsonSerializerOptions? options = null)
        {
            this.client = client;
            this.currentUserService = currentUserService;
            this.options = options;
        }

        protected virtual async Task SetAuthorization(HttpRequestMessage httpRequestMessage)
        {
            if (currentUserService != null && await currentUserService.IsAuthenticated())
            {
                var accessToken = await currentUserService.GetAccessToken();
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(currentUserService.Schema, accessToken);
            }
        }
    }
}