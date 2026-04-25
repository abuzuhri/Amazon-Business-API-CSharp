using CSharpAmazonBusinessAPI.Exceptions;
using CSharpAmazonBusinessAPI.Utils;
using Xunit;

namespace CSharpAmazonBusinessAPI.Tests;

public class AmazonBusinessConnectionTests
{
    private static AmazonBusinessCredential ValidCredential() => new()
    {
        ClientId = "amzn1.application-oa2-client.test",
        ClientSecret = "secret",
        RefreshToken = "Atzr|test",
        MarketPlace = MarketPlace.UnitedStates,
    };

    [Fact]
    public void Constructor_throws_unauthorized_when_credential_is_null()
    {
        Assert.Throws<AmazonBusinessUnauthorizedException>(() => new AmazonBusinessConnection(null!));
    }

    [Theory]
    [InlineData("ClientId")]
    [InlineData("ClientSecret")]
    [InlineData("RefreshToken")]
    public void Constructor_throws_invalid_input_when_required_field_is_missing(string field)
    {
        var c = ValidCredential();
        switch (field)
        {
            case "ClientId": c.ClientId = ""; break;
            case "ClientSecret": c.ClientSecret = ""; break;
            case "RefreshToken": c.RefreshToken = ""; break;
        }

        var ex = Assert.Throws<AmazonBusinessInvalidInputException>(() => new AmazonBusinessConnection(c));
        Assert.Contains(field, ex.Message);
    }

    [Fact]
    public void Constructor_throws_when_neither_marketplace_nor_marketplaceId_is_set()
    {
        var c = ValidCredential();
        c.MarketPlace = null!;
        c.MarketPlaceID = null!;

        var ex = Assert.Throws<AmazonBusinessInvalidInputException>(() => new AmazonBusinessConnection(c));
        Assert.Contains("MarketPlace", ex.Message);
    }

    [Fact]
    public void Constructor_resolves_marketplace_from_marketplaceID()
    {
        var c = ValidCredential();
        c.MarketPlace = null!;
        c.MarketPlaceID = "ATVPDKIKX0DER";

        var connection = new AmazonBusinessConnection(c);

        Assert.NotNull(connection.CurrentMarketPlace);
        Assert.Equal("ATVPDKIKX0DER", connection.CurrentMarketPlace.ID);
        Assert.Same(MarketPlace.UnitedStates, connection.CurrentMarketPlace);
    }

    [Fact]
    public void Connection_exposes_all_10_service_properties()
    {
        var connection = new AmazonBusinessConnection(ValidCredential());

        Assert.NotNull(connection.Documents);
        Assert.NotNull(connection.Cart);
        Assert.NotNull(connection.Applications);
        Assert.NotNull(connection.Ordering);
        Assert.NotNull(connection.PackageTracking);
        Assert.NotNull(connection.ProductSearch);
        Assert.NotNull(connection.Reconciliation);
        Assert.NotNull(connection.Reporting);
        Assert.NotNull(connection.ReportingLegacy);
        Assert.NotNull(connection.Users);
    }
}
