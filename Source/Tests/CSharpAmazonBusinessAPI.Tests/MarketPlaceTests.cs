using CSharpAmazonBusinessAPI.Utils;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class MarketPlaceTests
{
    [Theory]
    [InlineData("ATVPDKIKX0DER", "US", "NorthAmerica")]
    [InlineData("A2EUQ1WTGCTBG2", "CA", "NorthAmerica")]
    [InlineData("A1F83G8C2ARO7P", "GB", "Europe")]
    [InlineData("A21TJRUUN4KGV", "IN", "Europe")]
    [InlineData("A39IBJ37TRP1C6", "AU", "FarEast")]
    [InlineData("A1VC38T7YXB528", "JP", "FarEast")]
    public void GetMarketPlaceByID_returns_marketplace_with_correct_country_and_region(
        string id, string expectedCountryCode, string expectedRegionName)
    {
        var marketplace = MarketPlace.GetMarketPlaceByID(id);

        Assert.Equal(id, marketplace.ID);
        Assert.Equal(expectedCountryCode, marketplace.Country.Code);
        Assert.Contains(expectedRegionName, GetRegionName(marketplace));
    }

    [Fact]
    public void GetMarketPlaceByID_throws_for_unknown_id()
    {
        Assert.Throws<ArgumentException>(() => MarketPlace.GetMarketPlaceByID("BOGUS"));
    }

    [Fact]
    public void GetMarketplaceByCountryCode_returns_marketplace_for_known_country()
    {
        var marketplace = MarketPlace.GetMarketplaceByCountryCode("US");
        Assert.NotNull(marketplace);
        Assert.Equal("ATVPDKIKX0DER", marketplace!.ID);
    }

    [Fact]
    public void GetMarketplaceByCountryCode_returns_null_for_unknown_country()
    {
        Assert.Null(MarketPlace.GetMarketplaceByCountryCode("XX"));
    }

    [Theory]
    [InlineData("ATVPDKIKX0DER", "https://na.business-api.amazon.com", "https://sandbox.na.business-api.amazon.com")]
    [InlineData("A1PA6795UKMFR9", "https://eu.business-api.amazon.com", "https://sandbox.eu.business-api.amazon.com")]
    [InlineData("A1VC38T7YXB528", "https://jp.business-api.amazon.com", "https://sandbox.jp.business-api.amazon.com")]
    public void Marketplace_routes_to_correct_regional_host(string id, string prodHost, string sandboxHost)
    {
        var marketplace = MarketPlace.GetMarketPlaceByID(id);

        Assert.Equal(prodHost, marketplace.Region.HostUrl);
        Assert.Equal(sandboxHost, marketplace.Region.SandboxHostUrl);
    }

    private static string GetRegionName(MarketPlace marketplace) => marketplace.Region == Region.NorthAmerica ? "NorthAmerica"
        : marketplace.Region == Region.Europe ? "Europe"
        : marketplace.Region == Region.FarEast ? "FarEast"
        : "Unknown";
}
