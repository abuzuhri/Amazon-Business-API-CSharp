# ☕Amazon Business API C# 🚀 [![.NET](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/abuzuhri/Amazon-Business-API-CSharp/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/) 

.Net C# library for the Amazon Business API

This is an API Binding in .Net C# for the [Amazon Business API](https://developer-docs.amazon.com/amazon-business/docs).

This library is based on the output of [swagger-codegen](https://app.swaggerhub.com/home) with the [OpenAPI files provided by Amazon (Models)](https://developer-docs.amazon.com/amazon-business/docs/document-api-v1-model) and has been modified by the contributors.

The purpose of this package is to have an easy way of getting started with the Amazon Business API using C#




---
## Installation [![NuGet](https://img.shields.io/nuget/v/CSharpAmazonBusinessAPI.svg)](https://www.nuget.org/packages/CSharpAmazonBusinessAPI/)

```powershell
Install-Package CSharpAmazonBusinessAPI
```


### Tasks

The roadmap below covers the full Amazon Business developer surface ([docs index](https://developer-docs.amazon.com/amazon-business/docs)) and mirrors the structure used in the sister [Amazon-SP-API-CSharp](https://github.com/abuzuhri/Amazon-SP-API-CSharp) project. Foundation work is required before the per-API service wrappers can be built.

#### Foundation / scaffolding
- [x] **`MarketPlace` / `Region` / `Country` lookup tables** — 11 supported markets ([register-as-a-developer](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer)) across 3 regional hosts ([ab-api-endpoints](https://developer-docs.amazon.com/amazon-business/docs/ab-api-endpoints)): `na.business-api.amazon.com`, `eu.business-api.amazon.com`, `jp.business-api.amazon.com`.
- [x] **`AmazonBusinessCredential`** — LWA-only (`ClientId`, `ClientSecret`, `RefreshToken`, `MarketPlace` / `MarketPlaceID`, optional `Proxy`, `IsDebugMode`, `Environment`). No AWS keys.
- [x] **LWA token pipeline** — `LwaClient` POSTs to `https://api.amazon.com/auth/o2/token`; per-credential `AccessTokenCache` (thread-safe via `SemaphoreSlim`, refreshes 60s before expiry); `LwaAuthHandler` `DelegatingHandler` injects `x-amz-access-token` and retries once on 401.
- [x] **LWA client-secret rotation** — `AmazonBusinessCredential.RotateClientSecret(newSecret)` swaps the secret in place and invalidates the cache so the next call re-exchanges.
- [x] **`AmazonBusinessConnection` facade** — validates credentials, resolves `MarketPlaceID` → `MarketPlace`, builds a configured `HttpClient`, exposes API services (currently `Reports`).
- [x] **HTTP layer for NSwag-generated clients** — chained handlers `RateLimit → Auth → Debug → HttpClientHandler` wrap a shared `HttpClient` whose `BaseAddress` comes from `MarketPlace.Region`. Generated clients consume the `HttpClient` in their constructor.
- [x] **Exception types** — `AmazonBusinessException` base + `Unauthorized`, `InvalidInput`, `NotFound`, `QuotaExceeded`, `InternalError` subclasses. Each carries `StatusCode` + `ResponseBody`.
- [x] **Rate-limit handling** — `RateLimitHandler` `DelegatingHandler` honors `Retry-After` (delta or HTTP-date), exponential-backoff fallback, capped by `MaxThrottledRetryCount`; throws `AmazonBusinessQuotaExceededException` if exhausted.
- [x] **Sandbox toggle** — `AmazonBusinessCredential.Environment` (Sandbox/Production) switches `BaseAddress` between `Region.HostUrl` and `Region.SandboxHostUrl`.
- [x] **Debug logging** — `DebugLogHandler` pretty-prints request/response (with masked sensitive headers) when `IsDebugMode == true`. Routes through `ILogger` if `AmazonBusinessConnection` was constructed with an `ILoggerFactory`, else falls back to `Console`.

#### API surfaces

Each API needs: (1) OpenAPI spec dropped into `Source/CSharpAmazonBusinessAPI/OpenAPIs/`, (2) a new `<OpenApiReference>` entry in the csproj, (3) a hand-written `*Service` wrapper exposed off `AmazonConnection`, (4) examples in the sample app, (5) sandbox-mode tests.

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
- [ ] **[Amazon Business Punch-in](https://developer-docs.amazon.com/amazon-business/docs/punch-in-integration-guide)** — procurement-system handoff. Likely a separate helper class rather than a generated client.
- [ ] **[App Center authorization workflow](https://developer-docs.amazon.com/amazon-business/docs/app-center-authorization-workflow)** — distinct from Solution Provider Portal; needed for apps listed in [App Center](https://developer-docs.amazon.com/amazon-business/docs/what-is-amazon-business-app-center).
- [ ] **[Third-party website authorization](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow)** — alternate consent flow; document side-by-side with the standard flow.
- [ ] **[Amazon Business Integrations MCP Server](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-integrations-mcp-server)** — out of scope for the SDK itself, but worth a pointer in the README so users discover it.

#### Sample app & tests
- [ ] Replace the `Hello, World!` stub in `CSharpAmazonBusinessAPI.SampleCode/Program.cs` with runnable examples per API.
- [x] **`Tests/CSharpAmazonBusinessAPI.Tests`** — xUnit, 35 unit tests + 4 sandbox integration tests.
  - **Unit:** `MarketPlace` lookup + regional routing, `AmazonBusinessConnection` validation/marketplace resolution, `AccessTokenCache` (caching, invalidation, concurrent refresh, LWA failure), `LwaAuthHandler` (header injection, cache reuse, 401 retry-once with fresh token), `RateLimitHandler` (429 retry, `Retry-After` delta + HTTP-date, exhaustion → `AmazonBusinessQuotaExceededException`), `RotateClientSecret` flow, `ApiException` shape.
  - **Integration:** `SandboxIntegrationTests.cs` smoke-tests Documents, Reconciliation, ReportingLegacy, ProductSearch against the real sandbox. Skipped automatically when `AB_INTEGRATION_*` env vars aren't set; set them locally to exercise real wiring.
  - Run with `dotnet test` (or `dotnet test --filter Category=Integration` for just the sandbox tests).
- [x] [Amazon Business roles](https://developer-docs.amazon.com/amazon-business/docs/amazon-business-roles) reference — see the *Roles required per API* table in the [Usage](#usage) section. Roles are requested via the Developer Registration Access Form (DRAF) at app creation; the API surface won't authorize without the matching role.

---
## Keys
To get all keys needed you need to follow this step [Creating and configuring IAM policies and entities](https://developer-docs.amazon.com/amazon-business/docs/authorization-workflow) and then you need to [Registering your Application](https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer) then [Authorizing applications
](https://developer-docs.amazon.com/amazon-business/docs/create-app-client)
> :warning: **Use role ARN and dont use IAM user**


| Name | Description |
| --- | --- |
| Region | Marketplace region [List of Marketplaces](https://developer-docs.amazon.com/amazon-business/docs/marketplace-ids)|
| ClientId | Your amazon app id |
| ClientSecret | Your amazon app secret |
| RefreshToken | Check how to get [RefreshToken](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow) |


For more information about keys please check [New Amazon doc for create keys Developer ](https://developer-docs.amazon.com/) , If you are not registered as developer please [Register](https://developercentral.amazon.com/) to be able to create application. 


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
var details = await connection.PackageTracking.GetPackageTrackingDetailsAsync(
    orderId:    "114-2589187-9801025",
    shipmentId: "401971789238301",
    packageId:  "1",
    region:     Region.US);
```

### Product Search
For more, see [`ProductSearchSample.cs`](Source/CSharpAmazonBusinessAPI.SampleCode/ProductSearchSample.cs). The 5 ops have very wide parameter lists — call `.Client` directly.
```csharp
var results = await connection.ProductSearch.Client.SearchProductsRequestAsync(
    keywords: "office chair",
    productRegion: ProductRegion.US,
    shippingRegion: null, locale: "en_US", shippingPostalCode: null,
    facets: null, pageNumber: 0, pageSize: 24,
    groupTag: null, category: null, subCategory: null,
    availability: "InStockOnly",
    deliveryDay: null, eligibleForFreeShipping: null, primeEligible: null,
    upc: null, isbn: null, sku: null, ean: null,
    partNumber: null, oemPartNumber: null,
    searchRefinements: null, productFilters: null,
    x_amz_user_email: "buyer@example.com",
    inclusionsForProducts: null, inclusionsForOffers: null,
    sortKey: SortKey.RELEVANCE,
    minPrice: null, maxPrice: null);
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

### Out of scope: Integrated Quoting
Amazon's [Integrated Quoting workflow](https://developer-docs.amazon.com/amazon-business/docs/integrated-quoting) is a separate **cXML over HTTPS** integration with digital-certificate auth, used by enterprise eProcurement sourcing modules — it does not use the REST APIs this SDK wraps. If you need that flow, integrate it via your cXML stack alongside this library.




 
