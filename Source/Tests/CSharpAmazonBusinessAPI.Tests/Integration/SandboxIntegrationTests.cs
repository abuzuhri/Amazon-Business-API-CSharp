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
//
// Tests here are read-only and idempotent. Destructive ops (Ordering.PlaceOrderAsync,
// Users.CreateBusinessUserAccountAsync, Applications.RotateApplicationClientSecretAsync —
// the last one rotates your production secret) are intentionally NOT tested here. Add them
// to a separate suite when you're ready to opt-in.
[Trait("Category", "Integration")]
public class SandboxIntegrationTests : IClassFixture<SandboxFixture>
{
    private const string SkipReason = "AB_INTEGRATION_* env vars not set";

    private readonly SandboxFixture _fx;
    public SandboxIntegrationTests(SandboxFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Documents_GetReports_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);

        var response = await _fx.Connection!.Documents.GetReportsAsync(
            createdSince: DateTime.UtcNow.AddDays(-30));

        Assert.NotNull(response);
        Assert.NotNull(response.Reports);
    }

    [SkippableFact]
    public async Task Reconciliation_GetTransactions_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);

        var response = await _fx.Connection!.Reconciliation.GetTransactionsAsync(
            feedStartDate: DateTimeOffset.UtcNow.AddDays(-7),
            feedEndDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task ReportingLegacy_GetOrdersByOrderDate_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);

        var response = await _fx.Connection!.ReportingLegacy.GetOrdersByOrderDateAsync(
            startDate: DateTimeOffset.UtcNow.AddDays(-7),
            endDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task Reporting_GetOrderReports_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);

        var response = await _fx.Connection!.Reporting.GetOrderReportsAsync(
            orderStartDate: DateTimeOffset.UtcNow.AddDays(-7),
            orderEndDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task Reporting_GetShipmentReports_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);

        var response = await _fx.Connection!.Reporting.GetShipmentReportsAsync(
            orderStartDate: DateTimeOffset.UtcNow.AddDays(-30),
            orderEndDate: DateTimeOffset.UtcNow);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task ProductSearch_search_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);
        Skip.If(_fx.CustomerEmail is null, "AB_INTEGRATION_CUSTOMER_EMAIL not set (required for ProductSearch)");

        var response = await _fx.Connection!.ProductSearch.SearchProductsAsync(
            keywords: "stapler",
            customerEmail: _fx.CustomerEmail!,
            pageSize: 1);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task Cart_ListCarts_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);
        Skip.If(_fx.CustomerEmail is null, "AB_INTEGRATION_CUSTOMER_EMAIL not set (required for Cart)");

        var response = await _fx.Connection!.Cart.ListCartsAsync(
            customerEmail: _fx.CustomerEmail!,
            pageSize: 5);

        Assert.NotNull(response);
    }

    [SkippableFact]
    public async Task PackageTracking_GetPackageTrackingDetails_returns_response()
    {
        Skip.IfNot(_fx.IsConfigured, SkipReason);
        Skip.IfNot(_fx.HasPackageTrackingTarget,
            "AB_INTEGRATION_PT_ORDER_ID/SHIPMENT_ID/PACKAGE_ID not set (PackageTracking needs a known sandbox package)");

        var response = await _fx.Connection!.PackageTracking.GetPackageTrackingDetailsAsync(
            orderId: _fx.PackageTrackingOrderId!,
            shipmentId: _fx.PackageTrackingShipmentId!,
            packageId: _fx.PackageTrackingPackageId!);

        Assert.NotNull(response);
        Assert.NotNull(response.PackageTrackingDetails);
    }
}
