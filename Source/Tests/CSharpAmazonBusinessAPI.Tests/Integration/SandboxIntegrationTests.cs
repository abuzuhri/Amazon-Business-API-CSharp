using Xunit;

namespace CSharpAmazonBusinessAPI.Tests.Integration;

// Smoke tests against the real Amazon Business sandbox. Skipped automatically when
// AB_INTEGRATION_* env vars aren't set (see SandboxFixture). To run locally:
//
//   $env:AB_INTEGRATION_CLIENT_ID = "amzn1.application-oa2-client.XXXX"
//   $env:AB_INTEGRATION_CLIENT_SECRET = "XXXX"
//   $env:AB_INTEGRATION_REFRESH_TOKEN = "Atzr|XXXX"
//   $env:AB_INTEGRATION_CUSTOMER_EMAIL = "buyer@example.com"
//   dotnet test --filter Category=Integration
//
// These hit Amazon's sandbox endpoints (sandbox.{region}.business-api.amazon.com).
[Trait("Category", "Integration")]
public class SandboxIntegrationTests : IClassFixture<SandboxFixture>
{
    private readonly SandboxFixture _fx;
    public SandboxIntegrationTests(SandboxFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Documents_GetReports_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, "AB_INTEGRATION_* env vars not set");

        var response = await _fx.Connection!.Documents.GetReportsAsync(
            createdSince: DateTime.UtcNow.AddDays(-30));

        // Sandbox always returns a Reports collection — count may be 0 but the call must succeed.
        Assert.NotNull(response);
        Assert.NotNull(response.Reports);
    }

    [SkippableFact]
    public async Task Reconciliation_GetTransactions_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, "AB_INTEGRATION_* env vars not set");

        var response = await _fx.Connection!.Reconciliation.GetTransactionsAsync(
            feedStartDate: DateTimeOffset.UtcNow.AddDays(-7),
            feedEndDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task ReportingLegacy_GetOrdersByOrderDate_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, "AB_INTEGRATION_* env vars not set");

        var response = await _fx.Connection!.ReportingLegacy.GetOrdersByOrderDateAsync(
            startDate: DateTimeOffset.UtcNow.AddDays(-7),
            endDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task ProductSearch_search_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, "AB_INTEGRATION_* env vars not set");
        Skip.If(_fx.CustomerEmail is null, "AB_INTEGRATION_CUSTOMER_EMAIL not set (required for ProductSearch)");

        var response = await _fx.Connection!.ProductSearch.Client.SearchProductsRequestAsync(
            keywords: "stapler",
            productRegion: Model.ProductSearch.ProductRegion.US,
            shippingRegion: null, locale: "en_US", shippingPostalCode: null,
            facets: null, pageNumber: 0, pageSize: 1,
            groupTag: null, category: null, subCategory: null,
            availability: "InStockOnly",
            deliveryDay: null, eligibleForFreeShipping: null, primeEligible: null,
            upc: null, isbn: null, sku: null, ean: null,
            partNumber: null, oemPartNumber: null,
            searchRefinements: null, productFilters: null,
            x_amz_user_email: _fx.CustomerEmail,
            inclusionsForProducts: null, inclusionsForOffers: null,
            sortKey: Model.ProductSearch.SortKey.RELEVANCE,
            minPrice: null, maxPrice: null);

        Assert.NotNull(response);
    }
}
