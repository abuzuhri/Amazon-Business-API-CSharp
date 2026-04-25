using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class AccessTokenCacheTests
{
    private static AmazonBusinessCredential NewCredential() => new()
    {
        ClientId = "id",
        ClientSecret = "s",
        RefreshToken = "rt",
    };

    [Fact]
    public async Task Caches_token_across_calls_within_validity_window()
    {
        var credential = NewCredential();
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, """{"access_token":"first","token_type":"bearer","expires_in":3600}""");
        var lwa = TestableLwaClient.Create(stub);

        var t1 = await credential.TokenCache.GetAccessTokenAsync(credential, lwa);
        var t2 = await credential.TokenCache.GetAccessTokenAsync(credential, lwa);

        Assert.Equal("first", t1);
        Assert.Equal("first", t2);
        Assert.Equal(1, stub.Requests.Count);
    }

    [Fact]
    public async Task Invalidate_forces_next_call_to_re_exchange()
    {
        var credential = NewCredential();
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, """{"access_token":"first","token_type":"bearer","expires_in":3600}""");
        stub.Enqueue(HttpStatusCode.OK, """{"access_token":"second","token_type":"bearer","expires_in":3600}""");
        var lwa = TestableLwaClient.Create(stub);

        var t1 = await credential.TokenCache.GetAccessTokenAsync(credential, lwa);
        credential.TokenCache.Invalidate();
        var t2 = await credential.TokenCache.GetAccessTokenAsync(credential, lwa);

        Assert.Equal("first", t1);
        Assert.Equal("second", t2);
        Assert.Equal(2, stub.Requests.Count);
    }

    [Fact]
    public async Task Concurrent_callers_share_a_single_refresh()
    {
        var credential = NewCredential();
        var stub = new StubHttpMessageHandler();
        // Only one canned response — if the cache double-fires, the second caller will throw.
        stub.Enqueue(HttpStatusCode.OK, """{"access_token":"shared","token_type":"bearer","expires_in":3600}""");
        var lwa = TestableLwaClient.Create(stub);

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => credential.TokenCache.GetAccessTokenAsync(credential, lwa))
            .ToArray();
        var tokens = await Task.WhenAll(tasks);

        Assert.All(tokens, t => Assert.Equal("shared", t));
        Assert.Equal(1, stub.Requests.Count);
    }

    [Fact]
    public async Task Throws_unauthorized_when_lwa_returns_400()
    {
        var credential = NewCredential();
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        var lwa = TestableLwaClient.Create(stub);

        var ex = await Assert.ThrowsAsync<Exceptions.AmazonBusinessUnauthorizedException>(() =>
            credential.TokenCache.GetAccessTokenAsync(credential, lwa));
        Assert.Contains("LWA token exchange failed", ex.Message);
    }
}
