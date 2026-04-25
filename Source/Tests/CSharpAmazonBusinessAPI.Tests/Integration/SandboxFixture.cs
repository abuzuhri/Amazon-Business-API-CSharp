using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI.Tests.Integration;

// Holds a single sandbox AmazonBusinessConnection for the integration-test class.
// Reads credentials from env vars so CI / dev machines without secrets see the tests as Skipped
// rather than Failed.
//
// Required env vars to activate:
//   AB_INTEGRATION_CLIENT_ID
//   AB_INTEGRATION_CLIENT_SECRET
//   AB_INTEGRATION_REFRESH_TOKEN
//
// Optional:
//   AB_INTEGRATION_MARKETPLACE_ID   (default: ATVPDKIKX0DER / United States)
//   AB_INTEGRATION_CUSTOMER_EMAIL   (Cart, ProductSearch)
//   AB_INTEGRATION_PT_ORDER_ID      (PackageTracking — known sandbox order)
//   AB_INTEGRATION_PT_SHIPMENT_ID   (PackageTracking — known sandbox shipment)
//   AB_INTEGRATION_PT_PACKAGE_ID    (PackageTracking — known sandbox package)
public class SandboxFixture
{
    public string? ClientId { get; }
    public string? ClientSecret { get; }
    public string? RefreshToken { get; }
    public string MarketPlaceID { get; }
    public string? CustomerEmail { get; }

    public string? PackageTrackingOrderId { get; }
    public string? PackageTrackingShipmentId { get; }
    public string? PackageTrackingPackageId { get; }

    public AmazonBusinessConnection? Connection { get; }

    public bool IsConfigured => ClientId != null && ClientSecret != null && RefreshToken != null;

    public bool HasPackageTrackingTarget =>
        PackageTrackingOrderId != null && PackageTrackingShipmentId != null && PackageTrackingPackageId != null;

    public SandboxFixture()
    {
        ClientId = Environment.GetEnvironmentVariable("AB_INTEGRATION_CLIENT_ID");
        ClientSecret = Environment.GetEnvironmentVariable("AB_INTEGRATION_CLIENT_SECRET");
        RefreshToken = Environment.GetEnvironmentVariable("AB_INTEGRATION_REFRESH_TOKEN");
        MarketPlaceID = Environment.GetEnvironmentVariable("AB_INTEGRATION_MARKETPLACE_ID") ?? "ATVPDKIKX0DER";
        CustomerEmail = Environment.GetEnvironmentVariable("AB_INTEGRATION_CUSTOMER_EMAIL");

        PackageTrackingOrderId = Environment.GetEnvironmentVariable("AB_INTEGRATION_PT_ORDER_ID");
        PackageTrackingShipmentId = Environment.GetEnvironmentVariable("AB_INTEGRATION_PT_SHIPMENT_ID");
        PackageTrackingPackageId = Environment.GetEnvironmentVariable("AB_INTEGRATION_PT_PACKAGE_ID");

        if (IsConfigured)
        {
            Connection = new AmazonBusinessConnection(new AmazonBusinessCredential
            {
                ClientId = ClientId!,
                ClientSecret = ClientSecret!,
                RefreshToken = RefreshToken!,
                MarketPlace = MarketPlace.GetMarketPlaceByID(MarketPlaceID),
                Environment = AmazonBusinessCredential.Environments.Sandbox,
            });
        }
    }
}
