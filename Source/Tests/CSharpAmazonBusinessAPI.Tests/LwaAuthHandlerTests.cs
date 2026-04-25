using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class LwaAuthHandlerTests
{
    [Fact]
    public async Task Adds_x_amz_access_token_header_to_outbound_request()
    {
        var (credential, lwaStub) = CredentialAndLwaStub("token-A");
        var apiStub = new StubHttpMessageHandler();
        apiStub.Enqueue(HttpStatusCode.OK, """{"ok":true}""");

        var pipeline = BuildPipeline(credential, lwaStub, apiStub);
        var resp = await pipeline.GetAsync("https://api.business.test/anything");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var apiRequest = apiStub.Requests.Single();
        Assert.True(apiRequest.Headers.TryGetValues(LwaAuthHandler.AccessTokenHeader, out var values));
        Assert.Equal("token-A", values!.Single());
    }

    [Fact]
    public async Task Reuses_cached_token_across_requests()
    {
        var (credential, lwaStub) = CredentialAndLwaStub("token-A");
        var apiStub = new StubHttpMessageHandler();
        apiStub.Enqueue(HttpStatusCode.OK, "");
        apiStub.Enqueue(HttpStatusCode.OK, "");

        var pipeline = BuildPipeline(credential, lwaStub, apiStub);
        await pipeline.GetAsync("https://api.business.test/one");
        await pipeline.GetAsync("https://api.business.test/two");

        // 1 token exchange + 2 API calls = 1 LWA request, 2 API requests.
        Assert.Equal(1, lwaStub.Requests.Count);
        Assert.Equal(2, apiStub.Requests.Count);
    }

    [Fact]
    public async Task On_401_invalidates_cache_and_retries_once_with_fresh_token()
    {
        var credential = new AmazonBusinessCredential { ClientId = "id", ClientSecret = "s", RefreshToken = "rt" };
        var lwaStub = new StubHttpMessageHandler();
        // Two LWA exchanges expected: first for the original call, second after the 401 invalidates cache.
        lwaStub.Enqueue(HttpStatusCode.OK, """{"access_token":"old","token_type":"bearer","expires_in":3600}""");
        lwaStub.Enqueue(HttpStatusCode.OK, """{"access_token":"new","token_type":"bearer","expires_in":3600}""");

        var apiStub = new StubHttpMessageHandler();
        apiStub.Enqueue(HttpStatusCode.Unauthorized, """{"errors":[{"code":"Unauthorized"}]}""");
        apiStub.Enqueue(HttpStatusCode.OK, """{"ok":true}""");

        var pipeline = BuildPipeline(credential, lwaStub, apiStub);
        var resp = await pipeline.GetAsync("https://api.business.test/x");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(2, lwaStub.Requests.Count);
        Assert.Equal(2, apiStub.Requests.Count);

        var headers = apiStub.Requests.ToArray();
        Assert.Equal("old", headers[0].Headers.GetValues(LwaAuthHandler.AccessTokenHeader).Single());
        Assert.Equal("new", headers[1].Headers.GetValues(LwaAuthHandler.AccessTokenHeader).Single());
    }

    private static (AmazonBusinessCredential cred, StubHttpMessageHandler lwaStub) CredentialAndLwaStub(string token)
    {
        var credential = new AmazonBusinessCredential { ClientId = "id", ClientSecret = "s", RefreshToken = "rt" };
        var lwaStub = new StubHttpMessageHandler();
        lwaStub.Enqueue(HttpStatusCode.OK, $$"""{"access_token":"{{token}}","token_type":"bearer","expires_in":3600}""");
        return (credential, lwaStub);
    }

    private static HttpClient BuildPipeline(AmazonBusinessCredential cred, StubHttpMessageHandler lwaStub, StubHttpMessageHandler apiStub)
    {
        var lwaClient = TestableLwaClient.Create(lwaStub);
        var auth = new LwaAuthHandler(cred, lwaClient, apiStub);
        return new HttpClient(auth) { BaseAddress = new Uri("https://api.business.test/") };
    }
}

internal static class TestableLwaClient
{
    public static LwaClient Create(HttpMessageHandler handler)
    {
        var client = new LwaClient(proxy: null);
        var field = typeof(LwaClient).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((HttpClient)field.GetValue(client)!).Dispose();
        field.SetValue(client, new HttpClient(handler));
        return client;
    }
}
