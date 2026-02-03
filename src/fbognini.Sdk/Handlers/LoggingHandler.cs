using fbognini.Sdk.Exceptions;
using fbognini.Sdk.Extensions;
using fbognini.Sdk.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
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
        public string Query { get; init; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? RawRequest { get; set; }
        public IEnumerable<KeyValuePair<string, string>>? RequestHeaders { get; set; }
        public bool? IsSuccessStatusCode { get; set; }
        public int? StatusCode { get; set; }
        public string? ContentType { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string? RawResponse { get; set; }
        public IEnumerable<KeyValuePair<string, string>>? ResponseHeaders { get; set; }
        public double? ElapsedMilliseconds { get; set; }

        public Dictionary<string, object?> ToLoggingDictionary() => new()
        {
            ["IsSdk"] = true,
            [nameof(Sdk)] = Sdk,
            [nameof(BaseAddress)] = BaseAddress,
            [nameof(Method)] = Method,
            [nameof(Query)] = Query,
            [nameof(RequestDate)] = RequestDate,
            [nameof(RawRequest)] = RawRequest,
            [nameof(RequestHeaders)] = RequestHeaders,
            [nameof(IsSuccessStatusCode)] = IsSuccessStatusCode,
            [nameof(StatusCode)] = StatusCode,
            [nameof(ContentType)] = ContentType,
            [nameof(ResponseDate)] = ResponseDate,
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
            "application/hal+json",
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
                Query = request.RequestUri!.OriginalString,
                RequestDate = DateTime.UtcNow,
                RawRequest = await LoggingHandler.GetRawRequest(request.Content),
                RequestHeaders = LoggingHandler.GetHeaders(request.Headers)
            };

            try
            {
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    if (string.IsNullOrWhiteSpace(loggingPropertys.Sdk))
                    {
                        logger.LogInformation("Requesting {Method} {Query}", loggingPropertys.Method, loggingPropertys.Query);
                    }
                    else
                    {
                        logger.LogInformation("{Sdk} requesting {Method} {Query}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Query);
                    }
                }

                var message = await SendWithWatch();

                loggingPropertys.IsSuccessStatusCode = message.IsSuccessStatusCode;
                loggingPropertys.StatusCode = (int)message.StatusCode;
                loggingPropertys.ContentType = message.Content.Headers.ContentType?.MediaType;

                if (!string.IsNullOrWhiteSpace(loggingPropertys.ContentType) && SerializableContentType.Any(ct => loggingPropertys.ContentType.Contains(ct)))
                {
                    loggingPropertys.RawResponse = await message.Content.ReadAsStringAsync(cancellationToken);
                }

                loggingPropertys.ResponseHeaders = LoggingHandler.GetHeaders(message.Headers);

                var level = message.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    if (string.IsNullOrWhiteSpace(loggingPropertys.Sdk))
                    {
                        logger.Log(level, "{Method} {Query} responded {StatusCode} in {ElapsedMilliseconds}ms", loggingPropertys.Method, loggingPropertys.Query, loggingPropertys.StatusCode, loggingPropertys.ElapsedMilliseconds);
                    }
                    else
                    {
                        logger.Log(level, "{Sdk} {Method} {Query} responded {StatusCode} in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Query, loggingPropertys.StatusCode, loggingPropertys.ElapsedMilliseconds);
                    }
                }

                return message;

                async Task<HttpResponseMessage> SendWithWatch()
                {
                    try
                    {
                        return await base.SendAsync(request, cancellationToken);
                    }
                    finally
                    {
                        loggingPropertys.ResponseDate = DateTime.UtcNow;
                        loggingPropertys.ElapsedMilliseconds = (loggingPropertys.ResponseDate.Value - loggingPropertys.RequestDate).TotalMilliseconds;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Non posso sapere se si tratta di un annullo tramite CT oppure di un timeout perché sono all'interno di un message handler.
                // Il client potrà gestire la casistica con:
                // - PollyTimeout: catch (TimeoutRejectedException)
                // - HttpClientTimeout: catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException timeoutException)
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    logger.LogInformation("{Sdk} {Method} {Query} has been cancelled (or has timed out) in {ElapsedMilliseconds}ms", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Query, loggingPropertys.ElapsedMilliseconds);
                }

                throw;
            }
            catch (Exception ex)
            {
                using (logger.BeginScope(loggingPropertys.ToLoggingDictionary()))
                {
                    logger.LogError(ex, "{Sdk} failed to ask for {Method} {Query}", loggingPropertys.Sdk, loggingPropertys.Method, loggingPropertys.Query);
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

        private static List<KeyValuePair<string, string>> GetHeaders(HttpHeaders httpHeaders)
        {
            var baseHeaders = httpHeaders.GetType().GetProperties().Select(x => x.Name!).ToList();
            baseHeaders.Add("X-AspNet-Version");
            baseHeaders.Add("X-Content-Type-Options");
            baseHeaders.Add("X-Frame-Options");
            baseHeaders.Add("X-Powered-By");
            baseHeaders.Add("X-XSS-Protection");
            baseHeaders.Add("Cache-Control");
            baseHeaders.Add("Set-Cookie");
            baseHeaders.Add("Strict-Transport-Security");
            baseHeaders.Add("Transfer-Encoding");
            var headers = httpHeaders
                .Where(x => baseHeaders.Contains(x.Key) == false && x.Key.StartsWith("Access-Control") == false).ToList();

            return headers.SelectMany(x => x.Value, (header, value) => new KeyValuePair<string, string>(header.Key, value)).ToList();
        }

    }
}
