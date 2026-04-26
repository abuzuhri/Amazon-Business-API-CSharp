using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Exceptions;
using CSharpAmazonBusinessAPI.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class ErrorTranslationHandlerTests
{
    private const string AmazonErrorBody = """
        {
          "errors": [
            {
              "code": "InternalFailure",
              "message": "We encountered an internal error. Please try again.",
              "details": ""
            }
          ]
        }
        """;

    [Fact]
    public async Task Returns_response_unchanged_on_2xx()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var resp = await client.GetAsync("https://api.test/x");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("""{"ok":true}""", await resp.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, typeof(AmazonBusinessInvalidInputException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AmazonBusinessUnauthorizedException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(AmazonBusinessUnauthorizedException))]
    [InlineData(HttpStatusCode.NotFound, typeof(AmazonBusinessNotFoundException))]
    [InlineData((HttpStatusCode)429, typeof(AmazonBusinessQuotaExceededException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(AmazonBusinessInternalErrorException))]
    [InlineData((HttpStatusCode)503, typeof(AmazonBusinessInternalErrorException))]
    [InlineData((HttpStatusCode)504, typeof(AmazonBusinessInternalErrorException))]
    [InlineData((HttpStatusCode)418, typeof(AmazonBusinessException))]
    public async Task Maps_status_code_to_correct_exception_type(HttpStatusCode status, Type expectedType)
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(status, AmazonErrorBody);
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var ex = await Record.ExceptionAsync(() => client.GetAsync("https://api.test/x"));

        Assert.NotNull(ex);
        Assert.IsType(expectedType, ex);
    }

    [Fact]
    public async Task Carries_status_code_and_raw_body_on_thrown_exception()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.InternalServerError, AmazonErrorBody);
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var ex = await Assert.ThrowsAsync<AmazonBusinessInternalErrorException>(() =>
            client.GetAsync("https://api.test/anything"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal(AmazonErrorBody, ex.ResponseBody);
    }

    [Fact]
    public async Task Extracts_amazon_error_message_into_exception_message()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.BadRequest,
            """{"errors":[{"code":"InvalidInput","message":"Could not match input arguments","details":""}]}""");
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var ex = await Assert.ThrowsAsync<AmazonBusinessInvalidInputException>(() =>
            client.GetAsync("https://api.test/foo"));

        Assert.Contains("[InvalidInput]", ex.Message);
        Assert.Contains("Could not match input arguments", ex.Message);
        Assert.Contains("400", ex.Message);
        Assert.Contains("/foo", ex.Message);
    }

    [Fact]
    public async Task Falls_back_to_generic_message_when_body_is_not_amazon_error_shape()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.InternalServerError, "<html>Bad Gateway</html>");
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var ex = await Assert.ThrowsAsync<AmazonBusinessInternalErrorException>(() =>
            client.GetAsync("https://api.test/foo"));

        Assert.Contains("Amazon Business API call failed", ex.Message);
        Assert.Contains("500", ex.Message);
        Assert.Equal("<html>Bad Gateway</html>", ex.ResponseBody);
    }

    [Fact]
    public async Task Handles_empty_response_body()
    {
        var stub = new StubHttpMessageHandler();
        stub.Enqueue(HttpStatusCode.NotFound, "");
        var client = new HttpClient(new ErrorTranslationHandler(stub));

        var ex = await Assert.ThrowsAsync<AmazonBusinessNotFoundException>(() =>
            client.GetAsync("https://api.test/x"));

        Assert.Contains("404", ex.Message);
    }
}
