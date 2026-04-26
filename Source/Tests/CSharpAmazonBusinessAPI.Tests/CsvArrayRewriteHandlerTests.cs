using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class CsvArrayRewriteHandlerTests
{
    [Theory]
    [InlineData("https://api.test/x?foo=a&foo=b",                "https://api.test/x?foo=a,b")]
    [InlineData("https://api.test/x?foo=a&foo=b&foo=c",          "https://api.test/x?foo=a,b,c")]
    [InlineData("https://api.test/x?foo=a&bar=1&foo=b",          "https://api.test/x?foo=a,b&bar=1")]
    [InlineData("https://api.test/x?foo=a&bar=1&baz=2",          null)] // no repeats
    [InlineData("https://api.test/x?foo=a",                      null)]
    [InlineData("https://api.test/x",                            null)]
    [InlineData("https://api.test/x?reportTypes=A&reportTypes=B&processingStatuses=X&processingStatuses=Y",
                "https://api.test/x?reportTypes=A,B&processingStatuses=X,Y")]
    public void JoinRepeatedQueryKeys_groups_repeats_into_csv(string input, string? expected)
    {
        var actual = CsvArrayRewriteHandler.JoinRepeatedQueryKeys(new Uri(input));
        if (expected is null) Assert.Null(actual);
        else Assert.Equal(expected, actual!.ToString());
    }

    [Fact]
    public async Task SendAsync_rewrites_outbound_url()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, "{}");
        var handler = new CsvArrayRewriteHandler(stub);
        var client = new HttpClient(handler);

        await client.GetAsync("https://api.test/reports?reportTypes=A&reportTypes=B");

        Assert.Single(stub.Requests);
        var sent = stub.Requests.ToArray()[0];
        Assert.Equal("https://api.test/reports?reportTypes=A,B", sent.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_passes_through_when_no_repeats()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, "{}");
        var handler = new CsvArrayRewriteHandler(stub);
        var client = new HttpClient(handler);

        await client.GetAsync("https://api.test/reports?single=value");

        var sent = stub.Requests.ToArray()[0];
        Assert.Equal("https://api.test/reports?single=value", sent.RequestUri!.ToString());
    }
}
