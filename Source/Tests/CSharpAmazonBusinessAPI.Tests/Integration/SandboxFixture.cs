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
//   AB_INTEGRATION_MARKETPLACE_ID  (default: ATVPDKIKX0DER / United States)
//   AB_INTEGRATION_CUSTOMER_EMAIL  (only needed for Cart / ProductSearch ops)
public class SandboxFixture
{
    public string? ClientId { get; }
    public string? ClientSecret { get; }
    public string? RefreshToken { get; }
    public string MarketPlaceID { get; }
    public string? CustomerEmail { get; }

    public AmazonBusinessConnection? Connection { get; }

    public bool IsConfigured => ClientId != null && ClientSecret != null && RefreshToken != null;

    public SandboxFixture()
    {
        ClientId = Environment.GetEnvironmentVariable("AB_INTEGRATION_CLIENT_ID");
        ClientSecret = Environment.GetEnvironmentVariable("AB_INTEGRATION_CLIENT_SECRET");
        RefreshToken = Environment.GetEnvironmentVariable("AB_INTEGRATION_REFRESH_TOKEN");
        MarketPlaceID = Environment.GetEnvironmentVariable("AB_INTEGRATION_MARKETPLACE_ID") ?? "ATVPDKIKX0DER";
        CustomerEmail = Environment.GetEnvironmentVariable("AB_INTEGRATION_CUSTOMER_EMAIL");

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
