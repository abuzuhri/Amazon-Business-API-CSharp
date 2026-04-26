using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpAmazonBusinessAPI.Authentication
{
    // Amazon Business APIs expect array query parameters in CSV format
    // (e.g. ?reportTypes=A,B), but NSwag 13.18 generates multi-value format
    // (?reportTypes=A&reportTypes=B) regardless of the spec's collectionFormat.
    // The sandbox's pattern matcher rejects the multi format with 400
    // "Could not match input arguments". This handler joins repeated query
    // keys with commas just before the request hits the wire, so every
    // generated client serializes correctly without per-client patches.
    //
    // Idempotent — safe to run on retries.
    public class CsvArrayRewriteHandler : DelegatingHandler
    {
        public CsvArrayRewriteHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                var rewritten = JoinRepeatedQueryKeys(request.RequestUri);
                if (rewritten != null) request.RequestUri = rewritten;
            }
            return base.SendAsync(request, cancellationToken);
        }

        // Returns null if no rewriting was needed (no repeated keys / no query).
        internal static Uri JoinRepeatedQueryKeys(Uri uri)
        {
            var query = uri.Query;
            if (query.Length <= 1) return null;

            var parts = query.TrimStart('?').Split('&');
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var order = new List<string>();
            var anyRepeated = false;

            foreach (var part in parts)
            {
                var eq = part.IndexOf('=');
                var key = eq < 0 ? part : part.Substring(0, eq);
                var value = eq < 0 ? string.Empty : part.Substring(eq + 1);

                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    groups[key] = list;
                    order.Add(key);
                }
                else
                {
                    anyRepeated = true;
                }
                list.Add(value);
            }

            if (!anyRepeated) return null;

            var sb = new StringBuilder();
            for (var i = 0; i < order.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append(order[i]).Append('=').Append(string.Join(",", groups[order[i]]));
            }

            return new UriBuilder(uri) { Query = sb.ToString() }.Uri;
        }
    }
}
