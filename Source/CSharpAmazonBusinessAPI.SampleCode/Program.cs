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
var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
{
    ClientId = section["ClientId"]!,
    ClientSecret = section["ClientSecret"]!,
    RefreshToken = section["RefreshToken"]!,
    MarketPlaceID = section["MarketPlaceID"]!,
    Environment = AmazonBusinessCredential.Environments.Sandbox,
    IsDebugMode = true,
}, loggerFactory);

Console.WriteLine($"Region:    {connection.CurrentMarketPlace.Region.RegionName}");
Console.WriteLine($"Sandbox:   {connection.CurrentMarketPlace.Region.SandboxHostUrl}");
Console.WriteLine($"Marketplace: {connection.CurrentMarketPlace.Country.Name} ({connection.CurrentMarketPlace.ID})");
Console.WriteLine();


var carts = new CartSample(connection);
var customerEmail = section["CustomerEmail"]!;
var cartList = await carts.ListCartsAsync(customerEmail);
Console.WriteLine($"Cart.ListCarts → {cartList.CartDetailsList?.Count ?? 0} cart(s)");


var docs = new DocumentSample(connection);
var reports = await docs.GetReportsAsync();
Console.WriteLine($"Documents.GetReports → {reports.Reports?.Count ?? 0} report(s)");


// Live calls — uncomment once real credentials are in appsettings.json or User Secrets.
// (`dotnet user-secrets set "AmazonBusiness_US:ClientSecret" "..."` from this folder.)
//
// var docs = new DocumentSample(connection);
// var reports = await docs.GetReportsAsync();
// Console.WriteLine($"Documents.GetReports → {reports.Reports?.Count ?? 0} report(s)");
//
// var carts = new CartSample(connection);
// var customerEmail = section["CustomerEmail"]!;
// var cartList = await carts.ListCartsAsync(customerEmail);
// Console.WriteLine($"Cart.ListCarts → {cartList.Carts?.Count ?? 0} cart(s)");
