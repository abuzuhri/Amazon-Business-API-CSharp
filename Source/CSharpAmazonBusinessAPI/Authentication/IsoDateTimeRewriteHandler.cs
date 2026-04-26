using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpAmazonBusinessAPI.Authentication
{
    // NSwag 13.18 serializes DateTimeOffset query params with the "s" format specifier,
    // which produces ISO 8601 *without* a timezone designator (e.g. "2020-07-09T00:00:00").
    // Amazon Business APIs — and especially the static sandbox's exact-string pattern matcher
    // — expect the ISO form with a trailing "Z" (e.g. "2020-07-09T00:00:00Z"). Without the Z,
    // the sandbox returns 400 "Could not match input arguments" even when every other
    // parameter matches the documented match pattern.
    //
    // This handler rewrites query-string values that look like an ISO 8601 instant without
    // a timezone designator and appends "Z". Idempotent — safe across retries. Production
    // endpoints accept either form (real Amazon APIs are lenient), so this is safe to run
    // unconditionally.
    public class IsoDateTimeRewriteHandler : DelegatingHandler
    {
        // Matches yyyy-MM-ddTHH:mm:ss with optional .fff and no trailing offset/Z.
        private static readonly Regex IsoWithoutTimezone = new Regex(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$",
            RegexOptions.Compiled);

        public IsoDateTimeRewriteHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                var rewritten = AppendZToBareIsoDates(request.RequestUri);
                if (rewritten != null) request.RequestUri = rewritten;
            }
            return base.SendAsync(request, cancellationToken);
        }

        // Returns null if no rewriting was needed.
        internal static Uri AppendZToBareIsoDates(Uri uri)
        {
            var query = uri.Query;
            if (query.Length <= 1) return null;

            var parts = query.TrimStart('?').Split('&');
            var anyChanged = false;
            for (int i = 0; i < parts.Length; i++)
            {
                var eq = parts[i].IndexOf('=');
                if (eq < 0) continue;
                var key = parts[i].Substring(0, eq);
                var rawValue = parts[i].Substring(eq + 1);
                var decoded = Uri.UnescapeDataString(rawValue);
                if (IsoWithoutTimezone.IsMatch(decoded))
                {
                    parts[i] = key + "=" + Uri.EscapeDataString(decoded + "Z");
                    anyChanged = true;
                }
            }

            if (!anyChanged) return null;
            return new UriBuilder(uri) { Query = string.Join("&", parts) }.Uri;
        }
    }
}
