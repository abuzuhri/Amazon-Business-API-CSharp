using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.SampleCode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true)
    .Build();

var section = config.GetSection("AmazonBusiness_US");

// Note: console logger off here so the diagnostic summary at the bottom isn't drowned in
// debug output. Set IsDebugMode = true and pass loggerFactory back in to see request/response.
var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
{
    ClientId = section["ClientId"]!,
    ClientSecret = section["ClientSecret"]!,
    RefreshToken = section["RefreshToken"]!,
    MarketPlaceID = section["MarketPlaceID"]!,
    Environment = AmazonBusinessCredential.Environments.Sandbox,
    IsDebugMode = false,
});

Console.WriteLine($"Region:      {connection.CurrentMarketPlace.Region.RegionName}");
Console.WriteLine($"Sandbox:     {connection.CurrentMarketPlace.Region.SandboxHostUrl}");
Console.WriteLine($"Marketplace: {connection.CurrentMarketPlace.Country.Name} ({connection.CurrentMarketPlace.ID})");
Console.WriteLine();

var customerEmail = section["CustomerEmail"]!;
var results = new List<(string Service, string Op, string Status)>();

async Task Run(string service, string op, Func<Task<string>> action)
{
    try
    {
        var ok = await action();
        results.Add((service, op, "OK — " + ok));
    }
    catch (Exception ex)
    {
        results.Add((service, op, "FAILED — " + ex.GetType().Name + ": " + ex.Message));
    }
}

// ============================================================================
// SANDBOX-SUPPORTED APIs (per https://developer-docs.amazon.com/amazon-business/docs/amazon-business-api-sandbox)
// All parameters use values from each API's static-sandbox guide so the pattern matcher returns 200.
// ============================================================================

// --- Document API (invoice reports) ---
await Run("Document", "GetReports", async () =>
{
    var docs = new DocumentSample(connection);
    var r = await docs.GetReportsAsync();
    return $"{r.Reports?.Count ?? 0} report(s)";
});

await Run("Document", "GetReport('ID323')", async () =>
{
    var r = await connection.Documents.GetReportAsync("ID323");
    return $"reportId={r.ReportId}, status={r.ProcessingStatus}";
});

await Run("Document", "GetReportDocument", async () =>
{
    var r = await connection.Documents.GetReportDocumentAsync("0356cf79-b8b0-4226-b4b9-0ee058ea5760");
    return $"url length={r.Url?.Length}";
});

// --- Cart API ---
await Run("Cart", "ListCarts", async () =>
{
    var r = await connection.Cart.ListCartsAsync(customerEmail);
    return $"{r.CartDetailsList?.Count ?? 0} cart(s)";
});

await Run("Cart", "GetCart('cart-123')", async () =>
{
    var r = await connection.Cart.GetCartAsync("cart-123");
    return $"type={r.CartType}";
});

await Run("Cart", "GetItems('cart-123')", async () =>
{
    var r = await connection.Cart.GetItemsAsync("cart-123");
    return $"{r.Items?.Count ?? 0} item(s)";
});

// --- Reconciliation API ---
await Run("Reconciliation", "GetTransactions (Jul 2020)", async () =>
{
    // Sandbox-200 requires this exact date window per the spec.
    // IsoDateTimeRewriteHandler appends the trailing Z that NSwag's "s" formatter omits.
    var r = await connection.Reconciliation.GetTransactionsAsync(
        feedStartDate: DateTimeOffset.Parse("2020-07-09T00:00:00Z"),
        feedEndDate:   DateTimeOffset.Parse("2020-08-01T00:00:00Z"));
    return "response received";
});

// --- Package Tracking API ---
await Run("PackageTracking", "GetPackageTrackingDetails", async () =>
{
    // Documented sandbox-200 IDs from the spec
    var r = await connection.PackageTracking.GetPackageTrackingDetailsAsync(
        orderId:    "114-2589187-9801025",
        shipmentId: "401971789238301",
        packageId:  "1");
    return $"trackingStatus={r.PackageTrackingDetails?.TrackingStatus}";
});

// --- Product Search API ---
// Note: ProductSearch's spec has NO x-amzn-api-sandbox block, so the sandbox doesn't
// actually mock it despite Amazon's "supported in sandbox" claim. Always 400. Tested
// here for completeness; expect failure until production credentials.
await Run("ProductSearch", "SearchProducts (no sandbox spec)", async () =>
{
    var r = await connection.ProductSearch.SearchProductsAsync(
        keywords: "stapler",
        customerEmail: customerEmail,
        pageSize: 1);
    return "response received";
});

// --- Reporting API v2025-06-09 (current) ---
// Documented sandbox pattern requires exact dates + orderStatuses=["CLOSED"] + region=US.
await Run("Reporting", "GetOrderReports (Jan-Feb 2025, CLOSED)", async () =>
{
    var r = await connection.Reporting.GetOrderReportsAsync(
        orderStartDate: DateTimeOffset.Parse("2025-01-15T10:30:00Z"),
        orderEndDate:   DateTimeOffset.Parse("2025-02-15T10:30:00Z"),
        orderStatuses:  new[] { "CLOSED" });
    return "response received";
});

// --- Reporting API v2021-01-08 (legacy) ---
// Note: legacy spec has NO x-amzn-api-sandbox block — same situation as ProductSearch.
// Always 400 in sandbox. Will work in production.
await Run("ReportingLegacy", "GetOrdersByOrderDate (no sandbox spec)", async () =>
{
    var r = await connection.ReportingLegacy.GetOrdersByOrderDateAsync(
        startDate: DateTimeOffset.UtcNow.AddDays(-7),
        endDate:   DateTimeOffset.UtcNow);
    return "response received";
});

// ============================================================================
// NOT IN SANDBOX (per Amazon's docs):
//   - Ordering              — would place real orders. Test against production with trial-mode.
//   - Application Management — RotateApplicationClientSecret rotates your REAL secret. Never run accidentally.
//   - User Management        — would create real Amazon Business users.
// They're wired in the SDK and reachable as connection.Ordering / .Applications / .Users
// once you're ready to opt in deliberately.
// ============================================================================

// ----------------- summary -----------------
Console.WriteLine();
Console.WriteLine("================ Sandbox smoke-test summary ================");
foreach (var (service, op, status) in results)
{
    var marker = status.StartsWith("OK") ? "✓" : "✗";
    Console.WriteLine($"  {marker} {service,-18} {op,-35} {status}");
}
Console.WriteLine();
var okCount = results.Count(r => r.Status.StartsWith("OK"));
Console.WriteLine($"  Result: {okCount}/{results.Count} succeeded");
