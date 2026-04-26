using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Exceptions;
using Newtonsoft.Json.Linq;

namespace CSharpAmazonBusinessAPI.Authentication
{
    // Translates non-2xx HTTP responses into our AmazonBusinessException hierarchy before
    // they reach the NSwag-generated client. Without this handler, callers see NSwag's
    // generated ApiException<ErrorList> (a per-spec type buried in CSharpAmazonBusinessAPI.Model.*),
    // which leaks generated-code types into application error-handling code. This handler
    // throws our domain-level types instead — same hierarchy regardless of which API failed.
    //
    // Position in the chain: outermost. So it sees the FINAL response (after RateLimit's 429
    // retries and Auth's 401 retry-once), and throwing here prevents NSwag's caller from ever
    // seeing the response — bypassing its own ApiException-throwing path.
    //
    // Status code → exception:
    //   400        → AmazonBusinessInvalidInputException
    //   401, 403   → AmazonBusinessUnauthorizedException
    //   404        → AmazonBusinessNotFoundException
    //   429        → AmazonBusinessQuotaExceededException (rare here — RateLimitHandler usually catches first)
    //   5xx        → AmazonBusinessInternalErrorException
    //   other 4xx  → AmazonBusinessException (base)
    public class ErrorTranslationHandler : DelegatingHandler
    {
        public ErrorTranslationHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode) return response;

            string body = response.Content == null
                ? string.Empty
                : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var status = response.StatusCode;
            var method = request.Method;
            var uri = request.RequestUri;
            response.Dispose();

            var amazonMessage = TryExtractAmazonErrorMessage(body)
                ?? $"Amazon Business API call failed.";
            var fullMessage = $"{amazonMessage} ({(int)status} {status} on {method} {uri?.AbsolutePath})";

            int code = (int)status;
            if (status == HttpStatusCode.BadRequest)
                throw new AmazonBusinessInvalidInputException(fullMessage, status, body);
            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
                throw new AmazonBusinessUnauthorizedException(fullMessage, status, body);
            if (status == HttpStatusCode.NotFound)
                throw new AmazonBusinessNotFoundException(fullMessage, status, body);
            if (code == 429)
                throw new AmazonBusinessQuotaExceededException(fullMessage, status, body);
            if (code >= 500)
                throw new AmazonBusinessInternalErrorException(fullMessage, status, body);
            throw new AmazonBusinessException(fullMessage, status, body);
        }

        // Amazon Business returns errors in this shape:
        //   { "errors": [ { "code": "...", "message": "...", "details": "..." } ] }
        // Extract the first error's `message` (and `code` / `details` if present) for a
        // friendlier exception message. Returns null if the body isn't valid JSON or doesn't
        // match the shape — caller falls back to a generic message.
        private static string TryExtractAmazonErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                var json = JToken.Parse(body);
                var firstError = json["errors"]?.First;
                if (firstError == null) return null;

                var code = firstError.Value<string>("code");
                var message = firstError.Value<string>("message");
                var details = firstError.Value<string>("details");

                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(code)) parts.Add($"[{code}]");
                if (!string.IsNullOrEmpty(message)) parts.Add(message);
                if (!string.IsNullOrEmpty(details)) parts.Add($"({details})");
                return parts.Count > 0 ? string.Join(" ", parts) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
