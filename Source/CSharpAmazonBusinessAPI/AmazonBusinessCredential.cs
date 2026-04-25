using System.Net;
using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI
{
    public class AmazonBusinessCredential
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }

        public MarketPlace MarketPlace { get; set; }
        public string MarketPlaceID { get; set; }

        public IWebProxy Proxy { get; set; }
        public bool IsDebugMode { get; set; }
        public Environments Environment { get; set; } = Environments.Production;
        public int MaxThrottledRetryCount { get; set; } = 3;

        // Token cache lives on the credential so it survives across AmazonBusinessConnection
        // recreations and supports in-place secret rotation.
        internal AccessTokenCache TokenCache { get; } = new AccessTokenCache();

        public AmazonBusinessCredential() { }

        public AmazonBusinessCredential(string clientId, string clientSecret, string refreshToken)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RefreshToken = refreshToken;
        }

        // Swap to a freshly rotated LWA client secret without recreating AmazonBusinessConnection.
        // The cached access token is invalidated so the next API call exchanges the new secret.
        // See https://developer-docs.amazon.com/amazon-business/docs/lwa-client-secret-rotation
        public void RotateClientSecret(string newClientSecret)
        {
            if (string.IsNullOrEmpty(newClientSecret))
                throw new System.ArgumentException("New client secret cannot be empty.", nameof(newClientSecret));
            ClientSecret = newClientSecret;
            TokenCache.Invalidate();
        }

        public enum Environments
        {
            Sandbox,
            Production,
        }
    }
}
