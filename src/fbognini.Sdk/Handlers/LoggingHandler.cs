using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace fbognini.Sdk.Handlers
{
    public class LoggingProperys
    {
        public static bool IsSdk => true;
        public string Sdk { get; init; } = string.Empty;
        public string BaseAddress { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public string RequestUrl { get; init; } = string.Empty;
        public string? RawRequest { get; set; }
        public bool? IsSuccessStatusCode { get; set; }
        public int? StatusCode { get; set; }
        public string? ContentType { get; set; }
        public string? RawResponse { get; set; }
        public IEnumerable<KeyValuePair<string, string>>? ResponseHeaders { get; set; }
        public double? ElapsedMilliseconds { get; set; }

        public Dictionary<string, object?> ToLoggingDictionary() => new()
        {
            ["IsSdk"] = true,
            [nameof(Sdk)] = Sdk,
            [nameof(BaseAddress)] = BaseAddress,
            [nameof(Method)] = Method,
            [nameof(RequestUrl)] = RequestUrl,
            [nameof(RawRequest)] = RawRequest,
            [nameof(IsSuccessStatusCode)] = IsSuccessStatusCode,
            [nameof(StatusCode)] = StatusCode,
            [nameof(ContentType)] = ContentType,
            [nameof(RawResponse)] = RawResponse,
            [nameof(ResponseHeaders)] = ResponseHeaders,
            [nameof(ElapsedMilliseconds)] = ElapsedMilliseconds,
        };
    }

    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> logger;

        private static readonly List<string> SerializableContentType = new()
        {
            System.Net.Mime.MediaTypeNames.Application.Json,
            System.Net.Mime.MediaTypeNames.Application.Soap,
            System.Net.Mime.MediaTypeNames.Application.Xml,
            System.Net.Mime.MediaTypeNames.Text.Html,
            System.Net.Mime.MediaTypeNames.Text.Plain,
            System.Net.Mime.MediaTypeNames.Text.RichText,
            System.Net.Mime.MediaTypeNames.Text.Xml,
        };

        public LoggingHandler(ILogger<LoggingHandler> logger)
        {
            this.logger = logger;
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var loggingPropertys = new LoggingProperys
            {
                Sdk = LoggingHandler.GetFromOptions(request, BaseApiService.SdkOptionName),
                BaseAddress = LoggingHandler.GetFromOptions(request, BaseApiService.BaseAddressOptionName),
                Method = request.Method.Method,
                RequestUrl = request.RequestUri!.OriginalString,
                RawRequest = await LoggingHandler.GetRawRequest(request.Content)
            };

            try
            {

                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    logger.LogInformation("{Sdk} requesting {Method} {RequestUrl}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.RequestUrl);
                }

                var message = await SendWithWatch();


                loggingPropertys.IsSuccessStatusCode = message.IsSuccessStatusCode;
                loggingPropertys.StatusCode = (int)message.StatusCode;
                loggingPropertys.ContentType = message.Content.Headers.ContentType?.MediaType;

                if (!string.IsNullOrWhiteSpace(loggingPropertys.ContentType) && SerializableContentType.Any(ct => loggingPropertys.ContentType.Contains(ct)))
                {
                    loggingPropertys.RawResponse = await message.Content.ReadAsStringAsync(cancellationToken);
                }

                loggingPropertys.ResponseHeaders = LoggingHandler.GetResponseHeaders(message.Headers).ToList();

                var level = message.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
                if (logger.IsEnabled(level))
                {
                    using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                    {
                        logger.Log(level, "{Sdk} {Method} {RequestUrl} responded {StatusCode} in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.RequestUrl, loggingPropertys.StatusCode, loggingPropertys.ElapsedMilliseconds);
                    }
                }

                return message;

                async Task<HttpResponseMessage> SendWithWatch()
                {
                    var stopwatch = new Stopwatch();

                    try
                    {
                        stopwatch.Start();

                        return await base.SendAsync(request, cancellationToken);
                    }
                    finally
                    {
                        stopwatch.Stop();
                        loggingPropertys.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException timeoutException)
            {
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    logger.LogError(ex, "{Sdk} {Method} {RequestUrl} has timed out in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.RequestUrl, loggingPropertys.ElapsedMilliseconds);
                }

                throw;
            }
            catch (Exception ex)
            {
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    logger.LogError(ex, "{Sdk} failed to ask for {Method} {RequestUrl}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.RequestUrl);
                }

                throw;
            }
        }

        private static string GetFromOptions(HttpRequestMessage request, string key) => request.Options.TryGetValue(new HttpRequestOptionsKey<string>(key), out var sdk) ? sdk : string.Empty;

        private static async Task<string?> GetRawRequest(HttpContent? content)
        {
            if (content == null)
            {
                return null;
            }

            if (content is JsonContent json)
            {
                return JsonSerializer.Serialize(json.Value);
            }

            if (content is ByteArrayContent byteArray)
            {
                return await byteArray.ReadAsStringAsync();
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetResponseHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            var baseHeaders = httpResponseHeaders.GetType().GetProperties().Select(x => x.Name!).ToList();
            baseHeaders.Add("Set-Cookie");
            baseHeaders.Add("Cache-Control");
            baseHeaders.Add("X-AspNet-Version");
            baseHeaders.Add("X-Powered-By");
            baseHeaders.Add("Strict-Transport-Security");
            baseHeaders.Add("X-Content-Type-Options");
            baseHeaders.Add("X-Frame-Options");
            baseHeaders.Add("X-XSS-Protection");
            baseHeaders.Add("Transfer-Encoding");
            var headers = httpResponseHeaders
                .Where(x => baseHeaders.Contains(x.Key) == false && x.Key.StartsWith("Access-Control") == false).ToList();

            return headers.SelectMany(x => x.Value, (header, value) => new KeyValuePair<string, string>(header.Key, value));
        }

    }
}
