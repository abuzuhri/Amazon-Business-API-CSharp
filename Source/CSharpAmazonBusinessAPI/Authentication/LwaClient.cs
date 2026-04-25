using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Exceptions;
using Newtonsoft.Json;

namespace CSharpAmazonBusinessAPI.Authentication
{
    public class LwaClient
    {
        public const string TokenEndpoint = "https://api.amazon.com/auth/o2/token";

        private readonly HttpClient _httpClient;

        public LwaClient(IWebProxy proxy = null)
        {
            var handler = new HttpClientHandler();
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            _httpClient = new HttpClient(handler);
        }

        public async Task<LwaTokenResponse> RefreshAccessTokenAsync(
            AmazonBusinessCredential credential, CancellationToken cancellationToken = default)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", credential.RefreshToken),
                new KeyValuePair<string, string>("client_id", credential.ClientId),
                new KeyValuePair<string, string>("client_secret", credential.ClientSecret),
            });

            using (var response = await _httpClient.PostAsync(TokenEndpoint, form, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new AmazonBusinessUnauthorizedException(
                        $"LWA token exchange failed: {(int)response.StatusCode} {response.StatusCode}",
                        response.StatusCode, body);

                return JsonConvert.DeserializeObject<LwaTokenResponse>(body);
            }
        }
    }
}
