# Changelog

All notable changes to **CSharpAmazonBusinessAPI** are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project will adhere to [Semantic Versioning](https://semver.org/) once a `1.0` is tagged.

## [Unreleased]

This release brings the library from a stub (only the Reports/Document spec wired) to **full coverage of all 9 Amazon Business REST APIs**, with auth + retry + logging + onboarding helpers and 41 unit tests / 8 sandbox integration tests.

### Added

#### API surfaces — all 9 wired
- `connection.Documents` — Document API v1 (invoice reports). 5 ops: `GetReports` / `CreateReport` / `GetReport` / `CancelReport` / `GetReportDocument`.
- `connection.Cart` — Cart API v1. 7 ops (List / Get / GetItems / AddItems / ModifyItems / DeleteItems / EstimateCost). All take a `Country` (defaults from connection).
- `connection.Applications` — Application Management v1. `RotateApplicationClientSecretAsync` triggers Amazon to deliver a new LWA secret to the developer's SQS queue.
- `connection.Ordering` — Ordering API v1. `PlaceOrderAsync`, `OrderDetailsAsync`.
- `connection.PackageTracking` — Package Tracking v1. `GetPackageTrackingDetailsAsync`.
- `connection.ProductSearch` — Product Search v1. `SearchProductsAsync` + `GetProductByAsinAsync` convenience wrappers; raw `.Client` exposes the other 3 ops.
- `connection.Reconciliation` — Reconciliation v1. `GetTransactions` / `GetBatchInvoicePaymentDetails` / `GetInvoiceDetailsByOrderLineItems`.
- `connection.Reporting` — Reporting v2025-06-09 (current). 5 ops with `Country` parameter.
- `connection.ReportingLegacy` — Reporting v2021-01-08 (legacy). 2 ops, kept side-by-side for callers still on the old version.
- `connection.Users` — User Management v1. `CreateBusinessUserAccountAsync`.

#### Foundation
- **`AmazonBusinessConnection`** facade — single entry point with credential validation, marketplace-ID resolution, regional endpoint routing, and one property per API.
- **`AmazonBusinessCredential`** — LWA-only (`ClientId`, `ClientSecret`, `RefreshToken`, `MarketPlace` / `MarketPlaceID`, optional `Proxy`, `IsDebugMode`, `Environment`). Drops the legacy AWS `AccessKey`/`SecretKey`/`RoleArn` fields the original README example carried — Amazon Business uses LWA exclusively.
- **`MarketPlace` / `Region` / `Country`** lookup tables for the 11 Amazon Business markets (US / CA / MX / GB / DE / FR / ES / IT / IN / AU / JP) routed across 3 regional hosts (NA / EU / FE) with separate sandbox URLs.
- **LWA token pipeline** — `LwaClient` POSTs to `https://api.amazon.com/auth/o2/token`; per-credential `AccessTokenCache` (thread-safe via `SemaphoreSlim`, refreshes 60s before expiry); `LwaAuthHandler` `DelegatingHandler` injects `x-amz-access-token` and retries once on 401.
- **`LwaConsentHelper`** — OAuth code-flow helpers for one-time customer onboarding (`BuildAuthorizationUrl` + `ExchangeCodeForTokensAsync`). Used by both the website-authorization and App Center authorization flows.
- **`AmazonBusinessCredential.RotateClientSecret(newSecret)`** — swaps the LWA client secret in place and invalidates the cached token, so the next API call re-exchanges with the new secret.
- **Rate-limit handling** — `RateLimitHandler` retries on 429, honors `Retry-After` (delta or HTTP-date), exponential-backoff fallback, capped by `MaxThrottledRetryCount`. Throws `AmazonBusinessQuotaExceededException` when exhausted.
- **Sandbox toggle** — `AmazonBusinessCredential.Environment` (Sandbox / Production) switches the connection's `BaseAddress` between `Region.HostUrl` and `Region.SandboxHostUrl`.
- **Debug logging** — `DebugLogHandler` pretty-prints every request/response when `IsDebugMode == true`, with sensitive headers (`x-amz-access-token`, `Authorization`, `x-amz-security-token`) masked. Routes through `ILogger` if `AmazonBusinessConnection` was constructed with an `ILoggerFactory`, else `Console`.
- **Exception hierarchy** — `AmazonBusinessException` base + `Unauthorized` / `InvalidInput` / `NotFound` / `QuotaExceeded` / `InternalError` subclasses. Each carries `StatusCode` + `ResponseBody`. `Exceptions/ApiException.cs` provides the shared NSwag-compatible `ApiException` referenced via `AdditionalNamespaceUsages` so generated clients across all specs can share one type.

#### Spec management
- **`scripts/fetch_spec.py`** — pulls each API's OpenAPI spec from the embedded `ssr-props` script on Amazon's ReadMe-hosted docs (HTML scraping was unreliable; WebFetch summarizers truncated large specs).
- **Inline-enum dedupe pass** — same script post-processes each spec to hoist parameter enums that repeat across operations into shared `parameters/` `$ref`s, so NSwag generates one `Region` enum instead of `Region2`/`Region3`/.../`RegionN` per occurrence. When a parameter enum collides with an identically-valued schema definition, the script renames the schema (e.g. `definitions/Region` → `definitions/RegionCode`) so the parameter wins the clean name.
- Each generated client is namespaced under `CSharpAmazonBusinessAPI.Model.<Api>` to prevent type-name collisions across specs (every spec defines its own `Error` / `ErrorList` / `Anonymous`).

#### Wrapper layer over generated clients
- **`Country`-typed parameters** — service methods accept the project's `Country` class instead of NSwag-generated `Region` enums. `RegionConverter` (internal) maps `Country.AmazonCode` → whichever target enum the underlying op expects (handles the GB↔UK quirk: ISO uses `GB`, Amazon uses `UK`).
- **Default country from connection** — `country` parameter is optional on every wrapper method; falls back to `credential.MarketPlace.Country`. Override per-call only when needed.

#### Samples & tests
- **One `*Sample.cs` per service** under `Source/CSharpAmazonBusinessAPI.SampleCode/` (10 in total, plus a `CartToOrderSample` showing Product Search → Cart → Ordering as a cross-API workflow).
- **`Program.cs`** loads credentials from `appsettings.json` + User Secrets, prints resolved region/host/marketplace, and has commented-out live calls ready to enable.
- **`Source/Tests/CSharpAmazonBusinessAPI.Tests`** — xUnit suite. **41 unit tests** covering `MarketPlace` lookup + regional routing, `AmazonBusinessConnection` validation, `AccessTokenCache` (caching / invalidation / concurrent refresh / LWA failure), `LwaAuthHandler` (header injection / 401 retry-once), `RateLimitHandler` (`Retry-After` delta + HTTP-date / exhaustion → `QuotaExceeded`), `RotateClientSecret`, `ApiException` shape, `LwaConsentHelper` URL builder + arg validation. Mock `HttpMessageHandler` keeps everything offline.
- **8 sandbox integration tests** (`SkippableFact`) hitting `sandbox.{region}.business-api.amazon.com` for Documents / Reconciliation / ReportingLegacy / Reporting / ProductSearch / Cart / PackageTracking — skipped automatically when `AB_INTEGRATION_*` env vars aren't set. Destructive ops (`Ordering.PlaceOrder`, `Users.CreateBusinessUserAccount`, `Applications.RotateApplicationClientSecret`) intentionally not in the default suite.

### Removed

- AWS `AccessKey` / `SecretKey` / `RoleArn` from `AmazonBusinessCredential` and the original config example. Amazon Business has been LWA-only across SP-API and Business API since 2024; the legacy AWS-STS path is gone from Amazon's docs and the SDK.

### Out of scope

- **Integrated Quoting** — cXML over HTTPS with digital-certificate auth, used by enterprise eProcurement sourcing modules. Not a REST workflow; can be integrated alongside this library via your cXML stack.
- **Punch-in** — server-side endpoint your e-procurement system hosts (TLS 1.2+, shared-secret / cert auth, XML POST). Same situation as Integrated Quoting.
- **Amazon Business Integrations MCP Server** — Amazon-hosted, intended for AI-assistant integrations rather than this SDK. Linked from the README for discoverability.
