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
        public IEnumerable<KeyValuePair<string, IEnumerable<string>>>? Headers { get; set; }
        public IDictionary<string, object?>? Options { get; set; }
    }
}
