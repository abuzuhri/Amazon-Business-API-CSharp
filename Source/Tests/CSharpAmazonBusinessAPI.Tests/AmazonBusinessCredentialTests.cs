using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class AmazonBusinessCredentialTests
{
    [Fact]
    public void RotateClientSecret_updates_secret_and_invalidates_cached_token()
    {
        var credential = new AmazonBusinessCredential
        {
            ClientId = "id",
            ClientSecret = "old-secret",
            RefreshToken = "rt",
        };

        // Prime the cache via the LWA flow with a stub.
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK,
            """{"access_token":"first-token","token_type":"bearer","expires_in":3600}""");
        var client = new TestableLwaClient(stub);

        credential.TokenCache.GetAccessTokenAsync(credential, client).GetAwaiter().GetResult();

        // Rotate — should clear the cache so the next request re-exchanges.
        credential.RotateClientSecret("new-secret");
        Assert.Equal("new-secret", credential.ClientSecret);

        stub.Enqueue(HttpStatusCode.OK,
            """{"access_token":"second-token","token_type":"bearer","expires_in":3600}""");
        var token = credential.TokenCache.GetAccessTokenAsync(credential, client).GetAwaiter().GetResult();

        Assert.Equal("second-token", token);
        Assert.Equal(2, stub.Requests.Count);
    }

    [Fact]
    public void RotateClientSecret_throws_for_empty_secret()
    {
        var credential = new AmazonBusinessCredential { ClientId = "id", ClientSecret = "x", RefreshToken = "rt" };
        Assert.Throws<ArgumentException>(() => credential.RotateClientSecret(""));
    }

    // Wraps LwaClient so we can swap in our own HttpClient for tests.
    private class TestableLwaClient : LwaClient
    {
        public TestableLwaClient(HttpMessageHandler handler) : base(proxy: null)
        {
            // The base class created its own HttpClient; replace via reflection.
            var field = typeof(LwaClient).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            ((HttpClient)field.GetValue(this)!).Dispose();
            field.SetValue(this, new HttpClient(handler));
        }
    }
}
