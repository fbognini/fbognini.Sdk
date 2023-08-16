using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Models
{
    public class RequestOptions
    {
        public HttpRequestHeaders Headers { get; set; } = new HttpRequestMessage().Headers;
        public HttpRequestOptions Options { get; set; } = new HttpRequestMessage().Options;
    }
}
