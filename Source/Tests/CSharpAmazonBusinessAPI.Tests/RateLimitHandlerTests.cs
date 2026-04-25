using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Exceptions;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class RateLimitHandlerTests
{
    [Fact]
    public async Task Returns_response_immediately_on_2xx()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, "ok");

        var resp = await NewClient(stub, maxRetries: 3).GetAsync("https://x/");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(1, stub.Requests.Count);
    }

    [Fact]
    public async Task Retries_on_429_then_succeeds()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue((HttpStatusCode)429, "throttle", customize: r => r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1)));
        stub.Enqueue(HttpStatusCode.OK, "ok");

        var resp = await NewClient(stub, maxRetries: 3).GetAsync("https://x/");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(2, stub.Requests.Count);
    }

    [Fact]
    public async Task Throws_quota_exceeded_after_exhausting_retries()
    {
        var stub = new StubHttpMessageHandler();
        // 1 initial attempt + 2 retries = 3 responses, all 429.
        for (var i = 0; i < 3; i++)
            stub.Enqueue((HttpStatusCode)429, "still throttled",
                customize: r => r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1)));

        var ex = await Assert.ThrowsAsync<AmazonBusinessQuotaExceededException>(() =>
            NewClient(stub, maxRetries: 2).GetAsync("https://x/"));

        Assert.Equal((HttpStatusCode)429, ex.StatusCode);
        Assert.Equal("still throttled", ex.ResponseBody);
        Assert.Equal(3, stub.Requests.Count);
    }

    [Fact]
    public async Task Honors_retry_after_http_date_header()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue((HttpStatusCode)429, "wait", customize: r =>
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(1)));
        stub.Enqueue(HttpStatusCode.OK, "ok");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var resp = await NewClient(stub, maxRetries: 3).GetAsync("https://x/");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(900),
            $"Expected at least ~1s wait, got {sw.ElapsedMilliseconds}ms");
    }

    private static HttpClient NewClient(StubHttpMessageHandler stub, int maxRetries)
    {
        var credential = new AmazonBusinessCredential { MaxThrottledRetryCount = maxRetries };
        var handler = new RateLimitHandler(credential, stub);
        return new HttpClient(handler);
    }
}
