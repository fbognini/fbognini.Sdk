using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace fbognini.Sdk.Models
{
    public class RequestOptions
    {
        public HttpRequestHeaders Headers { get; set; } = new HttpRequestMessage().Headers;
        public HttpRequestOptions Options { get; set; } = new HttpRequestMessage().Options;
        public Encoding? Encoding { get; set; }

        public LogLevel? OverrideMinimumLogLevel { get; set; }
    }
}
