using System;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpAmazonBusinessAPI.Authentication
{
    public class AccessTokenCache
    {
        private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private string _accessToken;
        private DateTimeOffset _expiresAt;

        public async Task<string> GetAccessTokenAsync(
            AmazonBusinessCredential credential, LwaClient lwaClient, CancellationToken cancellationToken = default)
        {
            if (IsValid()) return _accessToken;

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsValid()) return _accessToken;

                var token = await lwaClient.RefreshAccessTokenAsync(credential, cancellationToken).ConfigureAwait(false);
                _accessToken = token.AccessToken;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
                return _accessToken;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Invalidate()
        {
            _accessToken = null;
            _expiresAt = DateTimeOffset.MinValue;
        }

        private bool IsValid() =>
            !string.IsNullOrEmpty(_accessToken) &&
            DateTimeOffset.UtcNow + RefreshSkew < _expiresAt;
    }
}
