using CSharpAmazonBusinessAPI.Exceptions;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class ApiExceptionTests
{
    [Fact]
    public void Carries_status_code_response_and_headers()
    {
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            ["x-amzn-RequestId"] = new[] { "req-123" },
        };

        var ex = new ApiException("boom", 503, "Service Unavailable", headers, innerException: null);

        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("Service Unavailable", ex.Response);
        Assert.Same(headers, ex.Headers);
        Assert.Contains("Status: 503", ex.Message);
        Assert.Contains("Service Unavailable", ex.Message);
    }

    [Fact]
    public void Truncates_long_response_body_in_message()
    {
        var longBody = new string('x', 1024);
        var ex = new ApiException("boom", 500, longBody, null!, null);

        // Truncated to 512 chars in the message (full body is still on .Response).
        Assert.True(ex.Message.IndexOf("xxxxx", StringComparison.Ordinal) > 0);
        Assert.Equal(1024, ex.Response.Length);
        // The truncated chunk inlined into Message should be 512 chars long.
        var inlined = ex.Message.Substring(ex.Message.IndexOf("Response: \n", StringComparison.Ordinal) + "Response: \n".Length);
        Assert.Equal(512, inlined.Length);
    }

    [Fact]
    public void Generic_carries_typed_result()
    {
        var headers = new Dictionary<string, IEnumerable<string>>();
        var ex = new ApiException<string>("boom", 400, "bad", headers, "the-result", null);

        Assert.Equal("the-result", ex.Result);
        Assert.Equal(400, ex.StatusCode);
    }
}
