using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpAmazonBusinessAPI.Authentication
{
    public class LwaAuthHandler : DelegatingHandler
    {
        public const string AccessTokenHeader = "x-amz-access-token";

        private readonly AmazonBusinessCredential _credential;
        private readonly LwaClient _lwaClient;

        public LwaAuthHandler(AmazonBusinessCredential credential, LwaClient lwaClient, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _lwaClient = lwaClient ?? throw new ArgumentNullException(nameof(lwaClient));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await AttachTokenAsync(request, cancellationToken).ConfigureAwait(false);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                _credential.TokenCache.Invalidate();
                await AttachTokenAsync(request, cancellationToken).ConfigureAwait(false);
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task AttachTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _credential.TokenCache
                .GetAccessTokenAsync(_credential, _lwaClient, cancellationToken)
                .ConfigureAwait(false);
            request.Headers.Remove(AccessTokenHeader);
            request.Headers.Add(AccessTokenHeader, token);
        }
    }
}
