using System.Web;
using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Exceptions;
using CSharpAmazonBusinessAPI.Utils;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class LwaConsentHelperTests
{
    [Fact]
    public void BuildBusinessAuthorizationUrl_uses_b2b_endpoint_with_applicationId_and_no_scope()
    {
        var url = LwaConsentHelper.BuildBusinessAuthorizationUrl(
            applicationId: "amzn1.sp.solution.abc-123",
            redirectUri:   "https://my.app/oauth/callback",
            state:         "csrf-token-123");

        var uri = new Uri(url);
        var qs = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("https://www.amazon.com/b2b/abws/oauth", uri.GetLeftPart(UriPartial.Path));
        Assert.Equal("amzn1.sp.solution.abc-123", qs["applicationId"]);
        Assert.Equal("https://my.app/oauth/callback", qs["redirect_uri"]);
        Assert.Equal("csrf-token-123", qs["state"]);

        // Critical: no scope, no client_id, no response_type — those are LWA-only and
        // would trigger lwa-invalid-parameter-bad-scope on the Business endpoint.
        Assert.Null(qs["scope"]);
        Assert.Null(qs["client_id"]);
        Assert.Null(qs["response_type"]);
    }

    [Theory]
    [InlineData("US", "https://www.amazon.com/b2b/abws/oauth")]
    [InlineData("DE", "https://www.amazon.de/b2b/abws/oauth")]
    [InlineData("JP", "https://www.amazon.co.jp/b2b/abws/oauth")]
    [InlineData("GB", "https://www.amazon.co.uk/b2b/abws/oauth")]
    [InlineData("AU", "https://www.amazon.com.au/b2b/abws/oauth")]
    public void BuildBusinessAuthorizationUrl_picks_correct_regional_endpoint(string countryCode, string expectedEndpoint)
    {
        var country = Country.US;
        if (countryCode != "US") country = typeof(Country).GetField(countryCode)!.GetValue(null) as Country;

        var url = LwaConsentHelper.BuildBusinessAuthorizationUrl(
            applicationId: "amzn1.sp.solution.x",
            redirectUri:   "https://x/cb",
            state:         "s",
            country:       country);

        Assert.StartsWith(expectedEndpoint + "?", url);
    }

    [Theory]
    [InlineData("applicationId")]
    [InlineData("redirectUri")]
    [InlineData("state")]
    public void BuildBusinessAuthorizationUrl_throws_on_missing_required_arg(string missing)
    {
        var applicationId = missing == "applicationId" ? "" : "amzn1.sp.solution.x";
        var redirectUri = missing == "redirectUri" ? "" : "https://x/cb";
        var state = missing == "state" ? "" : "s";

        var ex = Assert.Throws<ArgumentException>(() =>
            LwaConsentHelper.BuildBusinessAuthorizationUrl(applicationId, redirectUri, state));
        Assert.Equal(missing, ex.ParamName);
    }

    [Fact]
    public void BuildAuthorizationUrl_includes_all_required_query_parameters()
    {
        var url = LwaConsentHelper.BuildAuthorizationUrl(
            clientId: "amzn1.application-oa2-client.abc",
            redirectUri: "https://my.app/callback",
            state: "csrf-token-123");

        var uri = new Uri(url);
        var qs = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("https://www.amazon.com/ap/oa", uri.GetLeftPart(UriPartial.Path));
        Assert.Equal("amzn1.application-oa2-client.abc", qs["client_id"]);
        Assert.Equal("https://my.app/callback", qs["redirect_uri"]);
        Assert.Equal("csrf-token-123", qs["state"]);
        Assert.Equal("code", qs["response_type"]);
        Assert.Equal("profile", qs["scope"]);
    }

    [Fact]
    public void BuildAuthorizationUrl_url_encodes_special_characters_in_redirect_uri()
    {
        var url = LwaConsentHelper.BuildAuthorizationUrl(
            clientId: "id",
            redirectUri: "https://my.app/callback?env=prod&v=2",
            state: "s");

        Assert.Contains(
            "redirect_uri=https%3A%2F%2Fmy.app%2Fcallback%3Fenv%3Dprod%26v%3D2",
            url);
    }

    [Theory]
    [InlineData("clientId")]
    [InlineData("redirectUri")]
    [InlineData("state")]
    public void BuildAuthorizationUrl_throws_on_missing_required_arg(string missing)
    {
        var clientId = missing == "clientId" ? "" : "id";
        var redirectUri = missing == "redirectUri" ? "" : "https://my.app/cb";
        var state = missing == "state" ? "" : "s";

        var ex = Assert.Throws<ArgumentException>(() =>
            LwaConsentHelper.BuildAuthorizationUrl(clientId, redirectUri, state));
        Assert.Equal(missing, ex.ParamName);
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_throws_on_missing_args()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LwaConsentHelper.ExchangeCodeForTokensAsync("", "id", "secret", "https://x/cb"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LwaConsentHelper.ExchangeCodeForTokensAsync("c", "", "secret", "https://x/cb"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LwaConsentHelper.ExchangeCodeForTokensAsync("c", "id", "", "https://x/cb"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LwaConsentHelper.ExchangeCodeForTokensAsync("c", "id", "secret", ""));
    }

    // ExchangeCodeForTokensAsync's happy path is exercised end-to-end by the real LWA endpoint
    // in integration tests; the local pipeline here is the same FormUrlEncoded POST shape that
    // LwaClient already covers in AccessTokenCacheTests.

    [Fact(Skip = "Networked — exercises real LWA endpoint. Manual run only.")]
    public async Task ExchangeCodeForTokensAsync_returns_token_on_real_code()
    {
        // Set AB_CONSENT_* env vars and remove Skip to exercise.
        var token = await LwaConsentHelper.ExchangeCodeForTokensAsync(
            code: Environment.GetEnvironmentVariable("AB_CONSENT_CODE")!,
            clientId: Environment.GetEnvironmentVariable("AB_CONSENT_CLIENT_ID")!,
            clientSecret: Environment.GetEnvironmentVariable("AB_CONSENT_CLIENT_SECRET")!,
            redirectUri: Environment.GetEnvironmentVariable("AB_CONSENT_REDIRECT_URI")!);
        Assert.NotNull(token.AccessToken);
        Assert.NotNull(token.RefreshToken);
    }
}
