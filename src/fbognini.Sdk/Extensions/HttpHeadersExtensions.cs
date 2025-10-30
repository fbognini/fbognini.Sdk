using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Extensions;

public static class HttpHeadersExtensions
{
    public static void CopyTo(this HttpRequestHeaders source, HttpRequestHeaders target)
    {
        target.Accept.Clear();
        source.Accept.ToList().ForEach(target.Accept.Add);

        target.UserAgent.Clear();
        source.UserAgent.ToList().ForEach(target.UserAgent.Add);

        target.Upgrade.Clear();
        source.Upgrade.ToList().ForEach(target.Upgrade.Add);

        target.TransferEncoding.Clear();
        source.TransferEncoding.ToList().ForEach(target.TransferEncoding.Add);

        target.Trailer.Clear();
        source.Trailer.ToList().ForEach(target.Trailer.Add);

        target.TE.Clear();
        source.TE.ToList().ForEach(target.TE.Add);

        target.Referrer = source.Referrer;

        target.Range = source.Range;

        target.ProxyAuthorization = source.ProxyAuthorization;

        target.Pragma.Clear();
        source.Pragma.ToList().ForEach(target.Pragma.Add);

        target.MaxForwards = source.MaxForwards;

        target.IfUnmodifiedSince = source.IfUnmodifiedSince;

        target.IfRange = source.IfRange;

        target.Via.Clear();
        source.Via.ToList().ForEach(target.Via.Add);

        target.IfNoneMatch.Clear();
        source.IfNoneMatch.ToList().ForEach(target.IfNoneMatch.Add);

        target.IfMatch.Clear();
        source.IfMatch.ToList().ForEach(target.IfMatch.Add);

        target.Host = source.Host;

        target.From = source.From;

        target.ExpectContinue = source.ExpectContinue;

        target.Expect.Clear();
        source.Expect.ToList().ForEach(target.Expect.Add);

        target.Date = source.Date;

        target.ConnectionClose = source.ConnectionClose;

        target.Connection.Clear();
        source.Connection.ToList().ForEach(target.Connection.Add);

        target.CacheControl = source.CacheControl;

        target.Authorization = source.Authorization;

        target.AcceptLanguage.Clear();
        source.AcceptLanguage.ToList().ForEach(target.AcceptLanguage.Add);

        target.AcceptEncoding.Clear();
        source.AcceptEncoding.ToList().ForEach(target.AcceptEncoding.Add);

        target.AcceptCharset.Clear();
        source.AcceptCharset.ToList().ForEach(target.AcceptCharset.Add);

        target.IfModifiedSince = source.IfModifiedSince;

        target.Warning.Clear();
        source.Warning.ToList().ForEach(target.Warning.Add);

        foreach (var header in source)
        {
            if (IsKnownHeader(header.Key)) continue;

            target.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static bool IsKnownHeader(string key)
    {
        var known = new List<string>()
        {
            "Accept", 
            "User-Agent", 
            "Upgrade", 
            "Transfer-Encoding", 
            "Trailer", 
            "TE", 
            "Referer",
            "Range", 
            "Proxy-Authorization", 
            "Pragma", 
            "Max-Forwards", 
            "If-Unmodified-Since",
            "If-Range", 
            "Via", 
            "If-None-Match", 
            "If-Match", 
            "Host", 
            "From", 
            "Expect",
            "Date",
            "ConnectionClose",
            "Connection", 
            "Cache-Control", 
            "Authorization", 
            "Accept-Language", 
            "Accept-Encoding",
            "Accept-Charset", 
            "If-Modified-Since", 
            "Warning"
        };

        return known.Contains(key, StringComparer.OrdinalIgnoreCase);
    }
}