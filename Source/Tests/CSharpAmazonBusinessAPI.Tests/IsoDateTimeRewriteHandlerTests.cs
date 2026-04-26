using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class IsoDateTimeRewriteHandlerTests
{
    [Theory]
    // Bare ISO without timezone → append Z
    [InlineData("https://api.test/x?feedStartDate=2020-07-09T00:00:00",
                "https://api.test/x?feedStartDate=2020-07-09T00%3A00%3A00Z")]
    // Two date params, both rewritten
    [InlineData("https://api.test/x?feedStartDate=2020-07-09T00:00:00&feedEndDate=2020-08-01T00:00:00",
                "https://api.test/x?feedStartDate=2020-07-09T00%3A00%3A00Z&feedEndDate=2020-08-01T00%3A00%3A00Z")]
    // With fractional seconds
    [InlineData("https://api.test/x?createdSince=2025-04-15T12:30:45.123",
                "https://api.test/x?createdSince=2025-04-15T12%3A30%3A45.123Z")]
    // URL-encoded colons (NSwag escapes them) get rewritten too
    [InlineData("https://api.test/x?feedStartDate=2020-07-09T00%3A00%3A00",
                "https://api.test/x?feedStartDate=2020-07-09T00%3A00%3A00Z")]
    // Already has Z → no change
    [InlineData("https://api.test/x?feedStartDate=2020-07-09T00:00:00Z",                       null)]
    // Has timezone offset → no change
    [InlineData("https://api.test/x?feedStartDate=2020-07-09T00:00:00%2B05:00",                null)]
    // Non-date string left alone
    [InlineData("https://api.test/x?customerEmail=buyer@example.com",                          null)]
    // Mixed: date and non-date params
    [InlineData("https://api.test/x?region=US&feedStartDate=2020-07-09T00:00:00&pageSize=10",
                "https://api.test/x?region=US&feedStartDate=2020-07-09T00%3A00%3A00Z&pageSize=10")]
    // No query string at all
    [InlineData("https://api.test/x",                                                          null)]
    public void AppendZToBareIsoDates_handles_each_case(string input, string? expected)
    {
        var actual = IsoDateTimeRewriteHandler.AppendZToBareIsoDates(new Uri(input));
        if (expected is null) Assert.Null(actual);
        else Assert.Equal(expected, actual!.ToString());
    }

    [Fact]
    public async Task SendAsync_rewrites_outbound_url()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, "{}");
        var handler = new IsoDateTimeRewriteHandler(stub);
        var client = new HttpClient(handler);

        await client.GetAsync("https://api.test/x?feedStartDate=2020-07-09T00:00:00");

        var sent = stub.Requests.ToArray()[0].RequestUri!.ToString();
        Assert.Contains("2020-07-09T00", sent);
        Assert.Contains("00Z", sent);
    }

    [Fact]
    public async Task SendAsync_pass_through_when_no_dates()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, "{}");
        var client = new HttpClient(new IsoDateTimeRewriteHandler(stub));

        await client.GetAsync("https://api.test/x?region=US&pageSize=5");

        var sent = stub.Requests.ToArray()[0].RequestUri!.ToString();
        Assert.Equal("https://api.test/x?region=US&pageSize=5", sent);
    }
}
