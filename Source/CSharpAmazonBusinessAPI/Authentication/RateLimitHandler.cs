using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Exceptions;

namespace CSharpAmazonBusinessAPI.Authentication
{
    public class RateLimitHandler : DelegatingHandler
    {
        private const string RateLimitLimitHeader = "x-amzn-RateLimit-Limit";
        private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

        private readonly AmazonBusinessCredential _credential;

        public RateLimitHandler(AmazonBusinessCredential credential, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = 0;
            while (true)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != (HttpStatusCode)429)
                    return response;

                if (attempt >= _credential.MaxThrottledRetryCount)
                {
                    var body = response.Content == null
                        ? null
                        : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response.Dispose();
                    throw new AmazonBusinessQuotaExceededException(
                        $"Amazon Business API rate limit exceeded after {attempt} retries.",
                        (HttpStatusCode)429, body);
                }

                var delay = ResolveDelay(response, attempt);
                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                attempt++;
            }
        }

        private static TimeSpan ResolveDelay(HttpResponseMessage response, int attempt)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter != null)
            {
                if (retryAfter.Delta.HasValue) return Clamp(retryAfter.Delta.Value);
                if (retryAfter.Date.HasValue)
                {
                    var fromDate = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                    if (fromDate > TimeSpan.Zero) return Clamp(fromDate);
                }
            }

            // Exponential backoff: 1s, 2s, 4s, 8s, ... capped at MaxBackoff.
            var seconds = Math.Min(MaxBackoff.TotalSeconds, Math.Pow(2, attempt));
            return TimeSpan.FromSeconds(seconds);
        }

        private static TimeSpan Clamp(TimeSpan value)
        {
            if (value < MinBackoff) return MinBackoff;
            if (value > MaxBackoff) return MaxBackoff;
            return value;
        }
    }
}
