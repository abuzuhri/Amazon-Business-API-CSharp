# ☕Amazon Business API C# 🚀 [![.NET](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/) 

.NET C# library for the [Amazon Business API](https://developer-docs.amazon.com/amazon-business/docs). Wraps all 9 REST APIs (Document, Cart, Ordering, Product Search, Reconciliation, Reporting v2025-06-09 + v2021-01-08, User Management, Package Tracking, Application Management) behind a single `AmazonBusinessConnection`. LWA token refresh, rate-limit retry, regional endpoint routing, sandbox toggle, and debug logging are handled for you.

The generated clients are produced by [NSwag](https://github.com/RicoSuter/NSwag) from Amazon's published OpenAPI specs ([fetched](scripts/fetch_spec.py) verbatim from `developer-docs.amazon.com`); the wrapper layer hides the per-spec type duplication so callers see `Country.US` instead of NSwag-generated `Region2`/`Region3`/etc.

**Contents** — [Quick start](#quick-start) · [Configuration](#configuration) · [Service surface](#amazonbusinessconnection-exposes-one-property-per-api-surface) · [Onboarding (OAuth consent)](#onboard-a-customer-oauth-consent--refresh-token) · [Roles](#roles-required-per-api) · [Troubleshooting](#troubleshooting) · [Status](#status--roadmap)

---
## Installation [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/) [![NuGet Downloads](https://img.shields.io/nuget/dt/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/)

Latest version is published on nuget.org. Install with the .NET CLI:

```bash
dotnet add package CSharpAmazonBusinessAPI
```

…or via the legacy Package Manager Console:

```powershell
Install-Package CSharpAmazonBusinessAPI
```

…or in your `.csproj` directly:

```xml
<PackageReference Include="CSharpAmazonBusinessAPI" Version="*" />
```

Targets `netstandard2.0` — runs on .NET Framework 4.6.1+, .NET Core 2.0+, and every modern .NET (.NET 5/6/7/8/9/10).

## Quick start

```csharp
using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Utils;

var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
{
    ClientId     = "amzn1.application-oa2-client.XXXX",
    ClientSecret = "XXXX",
    RefreshToken = "Atzr|XXXX",
    MarketPlace  = MarketPlace.UnitedStates,
    // Environment = AmazonBusinessCredential.Environments.Sandbox,  // for testing
});

// First call — token refresh, regional host, auth header are all handled.
var reports = await connection.Documents.GetReportsAsync(
    createdSince: DateTime.UtcNow.AddDays(-7));

Console.WriteLine($"Got {reports.Reports?.Count ?? 0} report(s)");
```

See [Configuration](#configuration) for proxy / sandbox / debug-logging options, [Onboard a customer](#onboard-a-customer-oauth-consent--refresh-token) for the OAuth consent flow used during one-time customer onboarding, [Troubleshooting](#troubleshooting) when something doesn't behave as expected, and `Source/CSharpAmazonBusinessAPI.SampleCode/` for a per-API sample for every wrapper.

## Status & roadmap

The roadmap below covers the full Amazon Business developer surface ([docs index](https://developer-docs.amazon.com/amazon-business/docs)) and mirrors the structure used in the sister [Amazon-SP-API-CSharp](https://github.com/abuzuhri/Amazon-SP-API-CSharp) project. Foundation work is required before the per-API service wrappers can be built.

#### Foundation / scaffolding
- [x] **`MarketPlace` / `Region` / `Country` lookup tables** — 11 supported markets ([register-as-a-developer](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer)) across 3 regional hosts ([ab-api-endpoints](https://developer-docs.amazon.com/amazon-business/docs/ab-api-endpoints)): `na.business-api.amazon.com`, `eu.business-api.amazon.com`, `jp.business-api.amazon.com`.
- [x] **`AmazonBusinessCredential`** — LWA-only (`ClientId`, `ClientSecret`, `RefreshToken`, `MarketPlace` / `MarketPlaceID`, optional `Proxy`, `IsDebugMode`, `Environment`). No AWS keys.
- [x] **LWA token pipeline** — `LwaClient` POSTs to `https://api.amazon.com/auth/o2/token`; per-credential `AccessTokenCache` (thread-safe via `SemaphoreSlim`, refreshes 60s before expiry); `LwaAuthHandler` `DelegatingHandler` injects `x-amz-access-token` and retries once on 401.
- [x] **LWA client-secret rotation** — `AmazonBusinessCredential.RotateClientSecret(newSecret)` swaps the secret in place and invalidates the cache so the next call re-exchanges.
- [x] **`AmazonBusinessConnection` facade** — validates credentials, resolves `MarketPlaceID` → `MarketPlace`, builds a configured `HttpClient`, exposes one property per API surface (`Documents`, `Cart`, `Ordering`, `ProductSearch`, `Reconciliation`, `Reporting`, `ReportingLegacy`, `PackageTracking`, `Users`, `Applications`).
- [x] **HTTP layer for NSwag-generated clients** — chained handlers `RateLimit → Auth → Debug → HttpClientHandler` wrap a shared `HttpClient` whose `BaseAddress` comes from `MarketPlace.Region`. Generated clients consume the `HttpClient` in their constructor.
- [x] **Exception types** — `AmazonBusinessException` base + `Unauthorized`, `InvalidInput`, `NotFound`, `QuotaExceeded`, `InternalError` subclasses. Each carries `StatusCode` + `ResponseBody`.
- [x] **Rate-limit handling** — `RateLimitHandler` `DelegatingHandler` honors `Retry-After` (delta or HTTP-date), exponential-backoff fallback, capped by `MaxThrottledRetryCount`; throws `AmazonBusinessQuotaExceededException` if exhausted.
- [x] **Sandbox toggle** — `AmazonBusinessCredential.Environment` (Sandbox/Production) switches `BaseAddress` between `Region.HostUrl` and `Region.SandboxHostUrl`.
- [x] **Debug logging** — `DebugLogHandler` pretty-prints request/response (with masked sensitive headers) when `IsDebugMode == true`. Routes through `ILogger` if `AmazonBusinessConnection` was constructed with an `ILoggerFactory`, else falls back to `Console`.

#### API surfaces

Each API has: (1) OpenAPI spec under `Source/CSharpAmazonBusinessAPI/OpenAPIs/` (fetched by `scripts/fetch_spec.py`), (2) an `<OpenApiReference>` entry in the csproj, (3) a hand-written `*Service` wrapper exposed off `AmazonBusinessConnection`, (4) a sample under `Source/CSharpAmazonBusinessAPI.SampleCode/`, (5) sandbox-mode tests under `Source/Tests/`.

- [x] **[Application Management API v1](https://developer-docs.amazon.com/amazon-business/docs/application-management-api-reference)** — wired as `connection.Applications`. Single op (`RotateApplicationClientSecretAsync`) triggers Amazon to deliver a new LWA client secret to the developer's registered SQS queue.
- [x] **[Cart API v1](https://developer-docs.amazon.com/amazon-business/docs/cart-api-v1-reference)** — wired as `connection.Cart`. All 7 operations exposed (List/Get/AddItems/ModifyItems/DeleteItems/GetItems/EstimateCost). [Overview](https://developer-docs.amazon.com/amazon-business/docs/cart-api-overview) · [model](https://developer-docs.amazon.com/amazon-business/docs/cart-api-model) · [sandbox](https://developer-docs.amazon.com/amazon-business/docs/cart-api-static-sandbox-guide).
- [x] **[Document API v1](https://developer-docs.amazon.com/amazon-business/docs/document-api-v1-reference-1)** — wired as `connection.Documents`. Invoice-report retrieval via `GetReports/CreateReport/GetReport/CancelReport/GetReportDocument`. Region-specific guides: [NA invoices](https://developer-docs.amazon.com/amazon-business/docs/downloading-invoices-in-na), [EU/JP invoices](https://developer-docs.amazon.com/amazon-business/docs/downloading-invoices-in-eu-and-jp-regions). [Sandbox](https://developer-docs.amazon.com/amazon-business/docs/document-api-static-sandbox-guide).
- [x] **[Ordering API v1](https://developer-docs.amazon.com/amazon-business/docs/ordering-api-v1-reference)** — wired as `connection.Ordering` (`PlaceOrderAsync`, `OrderDetailsAsync`). [Overview](https://developer-docs.amazon.com/amazon-business/docs/ordering-api). Workflow guides:
  - [ ] [Placing an order](https://developer-docs.amazon.com/amazon-business/docs/placing-an-order)
  - [ ] [Order safeguards](https://developer-docs.amazon.com/amazon-business/docs/using-order-safeguards)
  - [ ] [Trial-mode validation](https://developer-docs.amazon.com/amazon-business/docs/validating-an-order-with-trial-mode)
  - [ ] [Order status retrieval](https://developer-docs.amazon.com/amazon-business/docs/retrieving-order-status)
  - [ ] [Multi-legal-entity configuration](https://developer-docs.amazon.com/amazon-business/docs/how-to-configure-ordering-api-for-multi-legal-entities)
  - [ ] [e-Invoicing enablement](https://developer-docs.amazon.com/amazon-business/docs/e-invoicing-for-ordering-api)
  - [ ] [Delivery preferences](https://developer-docs.amazon.com/amazon-business/docs/setting-delivery-preferences)
- [x] **[Package Tracking API v1](https://developer-docs.amazon.com/amazon-business/docs/package-tracking-api-v1-reference)** — wired as `connection.PackageTracking` (`GetPackageTrackingDetailsAsync`). Push notifications are out-of-band (SNS); this API covers pull-based detail retrieval. [Overview](https://developer-docs.amazon.com/amazon-business/docs/package-tracking-overview) · [push notifications](https://developer-docs.amazon.com/amazon-business/docs/package-tracking-push-notifications) · [sandbox](https://developer-docs.amazon.com/amazon-business/docs/package-tracking-api-static-sandbox-guide).
- [x] **[Product Search API v1](https://developer-docs.amazon.com/amazon-business/docs/product-search-api-v1-reference)** — wired as `connection.ProductSearch`. 5 ops with very wide parameter lists (search/get-product/get-offers/by-asin/by-offer-ids); use `.Client` directly for full IntelliSense. [Overview](https://developer-docs.amazon.com/amazon-business/docs/product-search-api-overview). Workflow guides:
  - [ ] [Initiating a search](https://developer-docs.amazon.com/amazon-business/docs/initiating-a-search)
  - [ ] [Refining results](https://developer-docs.amazon.com/amazon-business/docs/refining-search-results)
  - [ ] [Personalizing searches](https://developer-docs.amazon.com/amazon-business/docs/personalizing-searches)
  - [ ] [Product detail pages](https://developer-docs.amazon.com/amazon-business/docs/defining-product-detail-pages)
  - [ ] [Guided buying](https://developer-docs.amazon.com/amazon-business/docs/using-guided-buying)
  - [ ] [Sample queries](https://developer-docs.amazon.com/amazon-business/docs/sample-endpoints), [privileged fields](https://developer-docs.amazon.com/amazon-business/docs/product-search-api-privileged-fields), [categories](https://developer-docs.amazon.com/amazon-business/docs/product-search-api-categories)
- [x] **[Reconciliation API v1](https://developer-docs.amazon.com/amazon-business/docs/reconciliation-api-v1-reference)** — wired as `connection.Reconciliation` (`GetTransactions`, `GetBatchInvoicePaymentDetails`, `GetInvoiceDetailsByOrderLineItems`). [Overview](https://developer-docs.amazon.com/amazon-business/docs/reconciliation-api-overview). Workflow guides:
  - [ ] [Retrieving business transactions](https://developer-docs.amazon.com/amazon-business/docs/retrieving-business-transactions)
  - [ ] [Retrieving invoice details](https://developer-docs.amazon.com/amazon-business/docs/retrieving-invoice-details)
  - [ ] [Invoice details (JP)](https://developer-docs.amazon.com/amazon-business/docs/retrieving-invoice-details-in-jp-region)
- [x] **Reporting API** — both versions wired side-by-side; new is default, legacy still callable.
  - [x] [Reporting API v2025-06-09 (current)](https://developer-docs.amazon.com/amazon-business/docs/reporting-api-v2025-06-09-reference) — wired as `connection.Reporting` (5 ops: order reports, order line items, order reports by PO, shipment reports, shipment line items). Use `.Client` directly. [Model](https://developer-docs.amazon.com/amazon-business/docs/reporting-api-v2025-06-09-model).
  - [x] [Reporting API v2021-01-08 (legacy)](https://developer-docs.amazon.com/amazon-business/docs/reporting-api-v1-reference-1) — wired as `connection.ReportingLegacy` (`GetOrdersByOrderDateAsync`, `GetOrdersByOrderIdAsync`). [Model](https://developer-docs.amazon.com/amazon-business/docs/reporting-api-v1-model).
- [x] **[User Management API v1](https://developer-docs.amazon.com/amazon-business/docs/user-management-api-v1-reference)** — wired as `connection.Users` (`CreateBusinessUserAccountAsync`). [Overview](https://developer-docs.amazon.com/amazon-business/docs/user-management-api-overview) · [model](https://developer-docs.amazon.com/amazon-business/docs/user-management-v1-model).

#### Workflows & integrations
- [x] **REST cross-API sample** — see [`CartToOrderSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/CartToOrderSample.cs) for Product Search → Cart → Ordering. *Note:* Amazon's [Integrated Quoting workflow](https://developer-docs.amazon.com/amazon-business/docs/integrated-quoting) is a separate cXML/cert-auth integration, not a REST workflow — it's out of scope for this SDK.
- [x] **[Third-party website authorization](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow)** — `LwaConsentHelper.BuildAuthorizationUrl()` + `ExchangeCodeForTokensAsync()`. Use during one-time onboarding to collect a customer's refresh token, then persist it for `AmazonBusinessCredential`. **Runnable demo:** see [`Source/CSharpAmazonBusinessAPI.WebAuthSample`](Source/CSharpAmazonBusinessAPI.WebAuthSample) — ASP.NET Core minimal-API project with the full consent flow.
- [x] **[App Center authorization workflow](https://developer-docs.amazon.com/amazon-business/docs/app-center-authorization-workflow)** — same `LwaConsentHelper.ExchangeCodeForTokensAsync()` covers the LWA token-exchange step. The Amazon-initiated callback dance (`amazon_callback_uri`, `amazon_state` echoing, step-3 ack POST, step-6 return-to-App-Center redirect) is wired in the demo at `/appcenter/login-uri` and `/appcenter/oauth/callback` — see [`Source/CSharpAmazonBusinessAPI.WebAuthSample`](Source/CSharpAmazonBusinessAPI.WebAuthSample) for the working reference.
- [ ] ~~Punch-in~~ — **out of scope** (server-side cXML/SOAP-style endpoint your e-procurement system hosts, with TLS certs / shared-secret auth — same situation as Integrated Quoting). See [Punch-in integration guide](https://developer-docs.amazon.com/amazon-business/docs/punch-in-integration-guide) if you need to integrate the e-procurement side.
- [x] **[Amazon Business Integrations MCP Server](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-integrations-mcp-server)** — Amazon-hosted MCP server for AI assistants. Out of scope for the SDK; mentioned here for discoverability.

#### Sample app & tests
- [x] **`SampleCode/Program.cs`** loads credentials from `appsettings.json` + User Secrets and prints the resolved region/host/marketplace. Live calls per API are pre-wired and commented — uncomment after dropping in real credentials.
- [x] **`Tests/CSharpAmazonBusinessAPI.Tests`** — xUnit, 41 unit tests + 8 sandbox integration tests (read-only).
  - **Unit:** `MarketPlace` lookup + regional routing, `AmazonBusinessConnection` validation/marketplace resolution, `AccessTokenCache` (caching, invalidation, concurrent refresh, LWA failure), `LwaAuthHandler` (header injection, cache reuse, 401 retry-once with fresh token), `RateLimitHandler` (429 retry, `Retry-After` delta + HTTP-date, exhaustion → `AmazonBusinessQuotaExceededException`), `RotateClientSecret` flow, `ApiException` shape, `LwaConsentHelper` URL-builder + arg validation.
  - **Integration:** `SandboxIntegrationTests.cs` smoke-tests Documents, Reconciliation, ReportingLegacy, Reporting (GetOrderReports / GetShipmentReports), ProductSearch, Cart, PackageTracking against the real sandbox. Skipped automatically when `AB_INTEGRATION_*` env vars aren't set. **Destructive ops** (Ordering.PlaceOrder, Users.CreateBusinessUserAccount, Applications.RotateApplicationClientSecret) are intentionally not tested here — add a separate suite when you opt in.
  - Run with `dotnet test` (or `dotnet test --filter Category=Integration` for just the sandbox tests).
- [x] [Amazon Business roles](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-roles) reference — see the *Roles required per API* table in the [Usage](#usage) section. Roles are requested via the Developer Registration Access Form (DRAF) at app creation; the API surface won't authorize without the matching role.

---
## Keys

Amazon Business uses **Login with Amazon (LWA) only** — no AWS IAM, no STS, no role ARN. Onboarding flow:

1. [Register as a developer](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer) and request the [roles](#roles-required-per-api) your app needs (Developer Registration Access Form).
2. [Create an app client](https://developer-docs.amazon.com/amazon-business/docs/create-app-client) in the Solution Provider Portal — you'll receive a `ClientId` + `ClientSecret`.
3. [Authorize your app](https://developer-docs.amazon.com/amazon-business/docs/generate-refresh-token) (or use the [website-authorization workflow](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow)) to obtain a long-lived `RefreshToken` per Amazon Business customer.

| Field | Description |
| --- | --- |
| `MarketPlace` / `MarketPlaceID` | Target marketplace ([list](https://developer-docs.amazon.com/amazon-business/docs/marketplace-ids)). Determines regional endpoint (NA/EU/FE). |
| `ClientId` | Your app's `amzn1.application-oa2-client.…` ID. |
| `ClientSecret` | Your app's secret (rotatable — see [LWA client-secret rotation](#lwa-client-secret-rotation)). |
| `RefreshToken` | Long-lived token issued per customer after consent. Exchanged for a 1-hour access token automatically by the SDK. |


## Usage

### Configuration
See [`Program.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/Program.cs) for a runnable example that loads credentials from `appsettings.json` and User Secrets.

```CSharp
var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
{
    ClientId     = "amzn1.application-oa2-client.XXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    ClientSecret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    RefreshToken = "Atzr|XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    MarketPlace  = MarketPlace.UnitedStates,   // or: MarketPlaceID = "ATVPDKIKX0DER"
    Environment  = AmazonBusinessCredential.Environments.Sandbox,
    IsDebugMode  = true,
});
```

`AmazonBusinessConnection` exposes one property per API surface:

| Property | API |
| --- | --- |
| `Documents`       | Document API v1 (invoice reports) |
| `Cart`            | Cart API v1 |
| `Applications`    | Application Management API v1 |
| `Ordering`        | Ordering API v1 |
| `PackageTracking` | Package Tracking API v1 |
| `ProductSearch`   | Product Search API v1 |
| `Reconciliation`  | Reconciliation API v1 |
| `Reporting`       | Reporting API v2025-06-09 (current) |
| `ReportingLegacy` | Reporting API v2021-01-08 (legacy) |
| `Users`           | User Management API v1 |

Each service exposes typed methods plus a `.Client` property for direct access to the NSwag-generated client when you need parameters not on the wrapper.

### Configuration with a proxy

```csharp
var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
{
    ClientId = "...", ClientSecret = "...", RefreshToken = "...",
    MarketPlaceID = "ATVPDKIKX0DER",
    Proxy = new System.Net.WebProxy("http://xxx.xxx.xxx.xxx:xxxx")
    {
        Credentials = new System.Net.NetworkCredential("username", "password"),
    },
});
```

### Onboard a customer (OAuth consent → refresh token)
For SaaS apps and App Center listings: send the customer to Amazon's consent page, then exchange the returned `code` for a long-lived `refresh_token` and persist it. A runnable end-to-end ASP.NET Core demo lives in [`Source/CSharpAmazonBusinessAPI.WebAuthSample`](Source/CSharpAmazonBusinessAPI.WebAuthSample) — `dotnet run` from that folder, browse to `https://localhost:7271`, click *Connect*.

```csharp
// 1. Generate a CSRF token, then redirect the customer's browser here:
var url = LwaConsentHelper.BuildAuthorizationUrl(
    clientId:    "amzn1.application-oa2-client.XXXX",
    redirectUri: "https://my.app/oauth/callback",
    state:       Guid.NewGuid().ToString());

// 2. On callback, verify state matches, then exchange the code:
var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(
    code:         queryParams["code"],
    clientId:     "amzn1.application-oa2-client.XXXX",
    clientSecret: "...",
    redirectUri:  "https://my.app/oauth/callback");

// 3. Persist tokens.RefreshToken for this customer; pass it to AmazonBusinessCredential
//    going forward. (The access_token expires in 1h — the SDK handles renewal automatically.)
```

### LWA client-secret rotation

```csharp
// 1. Trigger Amazon to send a new secret to your registered SQS queue.
await connection.Applications.RotateApplicationClientSecretAsync();

// 2. After picking up the new secret, swap it in place — the cached access
//    token is invalidated automatically, so the next API call re-exchanges.
connection.Credentials.RotateClientSecret(newSecret);
```

### Document API — list and download invoice reports
For more, see [`DocumentSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/DocumentSample.cs).
```csharp
// List recent invoice reports (marketplaceIds defaults to credential).
var reports = await connection.Documents.GetReportsAsync(
    createdSince: DateTime.UtcNow.AddDays(-30));

// Create an invoice report, then poll for completion + download.
var reportId = (await connection.Documents.CreateReportAsync(new CreateReportSpecification
{
    ReportType    = "GET_FLAT_FILE_VAT_INVOICE_DATA_REPORT",
    DataStartTime = DateTime.UtcNow.AddDays(-30),
    DataEndTime   = DateTime.UtcNow,
})).ReportId;

var report = await connection.Documents.GetReportAsync(reportId);
if (report.ProcessingStatus == ProcessingStatus.DONE)
{
    var doc = await connection.Documents.GetReportDocumentAsync(report.ReportDocumentId);
    // doc.Url is a 5-minute presigned URL; doc.CompressionAlgorithm == GZIP if compressed.
}
```

### Cart API — list, get, add items
For more, see [`CartSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/CartSample.cs). The `country` parameter defaults to the connection's marketplace — pass `Country.X` explicitly only to override per call.
```csharp
// Country defaults from connection.Credentials.MarketPlace.Country.
var carts = await connection.Cart.ListCartsAsync(
    customerEmail: "buyer@example.com",
    pageSize: 25);

await connection.Cart.AddItemsAsync("cart-123", new AddItemsRequest
{
    Items = new List<AddItemRequest>
    {
        new AddItemRequest { ProductIdentifier = "B07HMBFZCZ", Quantity = 2 },
    },
});
```

### Reporting API v2025-06-09
For more, see [`ReportingSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ReportingSample.cs). The 5 ops have wrapper methods (country defaults from connection); `.Client` is also there for the raw generated surface.
```csharp
var orderReports = await connection.Reporting.GetOrderReportsAsync(
    orderStartDate: DateTimeOffset.UtcNow.AddDays(-7),
    orderEndDate:   DateTimeOffset.UtcNow);
```

### Reconciliation API — pull transactions
For more, see [`ReconciliationSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ReconciliationSample.cs).
```csharp
var transactions = await connection.Reconciliation.GetTransactionsAsync(
    feedStartDate: DateTimeOffset.UtcNow.AddDays(-30),
    feedEndDate:   DateTimeOffset.UtcNow);
```

### Package Tracking
For more, see [`PackageTrackingSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/PackageTrackingSample.cs).
```csharp
// Country defaults from the connection's marketplace.
var details = await connection.PackageTracking.GetPackageTrackingDetailsAsync(
    orderId:    "114-2589187-9801025",
    shipmentId: "401971789238301",
    packageId:  "1");
```

### Product Search
For more, see [`ProductSearchSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ProductSearchSample.cs). The two most common ops have wrappers; the other three (`SearchOffersRequest`, `GetProductsByAsins`, `GetOffersByOfferIds`) are reachable via `.Client`.
```csharp
var results = await connection.ProductSearch.SearchProductsAsync(
    keywords:      "office chair",
    customerEmail: "buyer@example.com",
    pageSize:      24,
    sortKey:       SortKey.RELEVANCE);

var product = await connection.ProductSearch.GetProductByAsinAsync(
    asin:          "B07HMBFZCZ",
    customerEmail: "buyer@example.com");
```

### Other surfaces
- **Ordering** — [`OrderingSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/OrderingSample.cs)
- **Reporting Legacy v2021-01-08** — [`ReportingLegacySample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ReportingLegacySample.cs)
- **User Management** — [`UserManagementSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/UserManagementSample.cs)
- **Application Management** — [`ApplicationManagementSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ApplicationManagementSample.cs)

### Enable debug mode
Set `IsDebugMode = true` on the credential. Pretty-prints request and response (with sensitive headers masked) for every outbound call. Pass an `ILoggerFactory` to `AmazonBusinessConnection` to route through `ILogger` instead of `Console`.

### Roles required per API

Each API call needs a specific Amazon Business role granted at app-creation time via the [Developer Registration Access Form](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer). Calling without the matching role returns 403. ([Source](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-roles).)

| Role | API surfaces (this SDK) |
| --- | --- |
| **Business Product Catalog** | `connection.ProductSearch` |
| **Amazon Business Analytics** | `connection.Reporting`, `connection.ReportingLegacy` |
| **Business Purchase Reconciliation** | `connection.Reconciliation`, `connection.Documents` |
| **Amazon Business Order Placement** | `connection.Ordering`, `connection.Cart`, `connection.PackageTracking` |
| **User Management** *(offline approval required)* | `connection.Users` |
| *(no role required)* | `connection.Applications` — uses standard LWA scope |

All roles are available across NA, EU, and FE marketplaces.

### Troubleshooting

#### `400 InvalidInput "Could not match input arguments"` in sandbox

Amazon Business's static sandbox does **exact** parameter matching, not subset matching. Sending any extra query parameter — even a sensible-looking one like `marketplaceIds` — breaks the match. Each operation has a documented sandbox match block in its OpenAPI spec under `responses.200.x-amzn-api-sandbox.static[].request.parameters`; send only those parameters with their literal values to get a 200.

Common offenders this SDK fixes automatically:
- **Repeated query keys** (`?foo=a&foo=b`) — NSwag's default; sandbox needs csv (`?foo=a,b`). Handled by `CsvArrayRewriteHandler`.
- **Bare ISO timestamps** (`2020-07-09T00:00:00`) — NSwag's `"s"` format omits the timezone designator; sandbox needs the trailing `Z`. Handled by `IsoDateTimeRewriteHandler`.
- **Auto-defaulted `marketplaceIds`** in `Documents.GetReports` — defaults from your credential's marketplace, but the sandbox doesn't expect it. Pass `marketplaceIds: Array.Empty<string>()` explicitly to skip the default for sandbox testing.

#### `500 InternalFailure` with empty `details`

Amazon's sandbox returns generic 500s for several scenarios where you'd hope for a clearer error. Check, in order:

1. **Role not granted** on your sandbox app. Each API requires a specific role (see [Roles required per API](#roles-required-per-api)). Confirm in [Solution Provider Portal](https://solutionproviderportal.amazon.com/) → your app → Roles. The fastest signal: if some APIs work and others 500, it's role-shaped — APIs sharing a role will fail together.
2. **Cart in particular is broken in sandbox.** Even with the documented `?region=US` and `cart-123` values, all Cart ops return 500 InternalFailure. This is an Amazon-side issue (not the SDK); production calls work fine. If you need this fixed, open a ticket via SPP support.
3. **Sandbox app not fully provisioned.** If *every* API 500s, the sandbox refresh token may not be properly bound. Email Amazon Business Support with your `applicationId` and a failed `x-amzn-RequestId`.

#### Operations that don't actually work in sandbox (despite Amazon's docs claiming so)

These have no `x-amzn-api-sandbox` block in their OpenAPI spec — the sandbox doesn't mock them and will always 400:
- `ProductSearch.SearchProducts` and the rest of Product Search
- `ReportingLegacy` (Reporting v2021-01-08) — both operations

They work fine against production endpoints. For local validation, hit production with `Environment.Production` once you have approved production credentials.

#### Operations not available in sandbox at all

Per Amazon's [sandbox docs](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-api-sandbox), three APIs are intentionally not in sandbox because they're destructive:
- `connection.Ordering` — would place real orders
- `connection.Applications.RotateApplicationClientSecretAsync` — rotates your real production secret even from a sandbox-context call
- `connection.Users.CreateBusinessUserAccountAsync` — creates real Amazon Business users

The SDK exposes them; you just need to opt into testing them deliberately against production.

#### `lwa-invalid-parameter-bad-scope` on the OAuth consent page

You're sending the customer to `https://www.amazon.com/ap/oa` (the standard LWA endpoint) instead of `https://www.amazon.<tld>/b2b/abws/oauth` (the Amazon Business endpoint). Use `LwaConsentHelper.BuildBusinessAuthorizationUrl(applicationId, redirectUri, state, country)` — not `BuildAuthorizationUrl`. The Business endpoint takes the SPP `applicationId` (`amzn1.sp.solution.*`), not the LWA `client_id`, and accepts no `scope` parameter.

#### "We cannot connect this account" on Amazon's consent page

Your sandbox app **doesn't go through the consent flow** — the sandbox refresh token is generated directly in SPP (Action → Create token), not via OAuth. The website-authorization workflow is for **production** customer onboarding, where real Amazon Business admins authorize your app for their org. Sandbox apps lack the registration metadata to consent against real accounts.

#### NSwag-generated `ApiException<ErrorList>` leaks into application code

If you see this, you're on a build before the `ErrorTranslationHandler` was wired in (pre-0.1.1). Upgrade to the current package — non-2xx responses now throw `AmazonBusinessException` subclasses (`InvalidInput` / `Unauthorized` / `NotFound` / `QuotaExceeded` / `InternalError`) carrying `StatusCode` + `ResponseBody` + a parsed Amazon-error message.

#### Push protection blocks `git push` with "Repository contains secrets"

You committed real LWA credentials. Rotate them in SPP **first** (they're already exposed). Then either re-create the bad commit without the secret files (`git rm --cached <file>`, amend or rebase) or use `git filter-repo` to scrub the file from the entire history. Don't take the "allow this secret" escape hatch — secrets in git history are permanent.

### Out of scope: Integrated Quoting
Amazon's [Integrated Quoting workflow](https://developer-docs.amazon.com/amazon-business/docs/integrated-quoting) is a separate **cXML over HTTPS** integration with digital-certificate auth, used by enterprise eProcurement sourcing modules — it does not use the REST APIs this SDK wraps. If you need that flow, integrate it via your cXML stack alongside this library.




 
