# Changelog

All notable changes to **CSharpAmazonBusinessAPI** are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project will adhere to [Semantic Versioning](https://semver.org/) once a `1.0` is tagged.

> **Release process:** bump `<Version>` in `Source/CSharpAmazonBusinessAPI/CSharpAmazonBusinessAPI.csproj`, move the `[Unreleased]` block under a new dated heading, then `git tag vX.Y.Z && git push --tags`. The `Release` workflow (`.github/workflows/nuget.yml`) packs and pushes to nuget.org on tag push.

## [0.1.1] — 2026-04-26

Sandbox-validation pass: every fix needed to get real sandbox calls returning 200. End-to-end verified against 6 of Amazon's 11 sandbox-supported operations (the other 5 fail for reasons outside the SDK — Cart sandbox bug + 2 ops Amazon doesn't actually mock despite their docs claiming so). **84 unit tests + 9 sandbox integration tests.**

### Added

- **`CsvArrayRewriteHandler`** — `DelegatingHandler` that joins repeated query keys into csv format. NSwag 13.18 emits the multi format (`?foo=a&foo=b`) regardless of the spec's `collectionFormat`; Amazon's sandbox pattern matcher requires csv (`?foo=a,b`). Same outbound URL on the wire either way for production endpoints, so safe to run unconditionally.
- **`IsoDateTimeRewriteHandler`** — appends a trailing `Z` to bare ISO 8601 timestamps in query strings. NSwag's `"s"` format specifier omits the timezone designator (`2020-07-09T00:00:00`); the sandbox's exact-string matcher and most spec patterns require `Z` (`2020-07-09T00:00:00Z`). Idempotent — already-zoned values pass through.
- **`ErrorTranslationHandler`** — outermost handler. Translates non-2xx HTTP responses into `AmazonBusinessException` subclasses *before* NSwag's generated `ApiException<ErrorList>` reaches the caller. Status → exception map: 400 → `InvalidInput`, 401/403 → `Unauthorized`, 404 → `NotFound`, 429 → `QuotaExceeded`, 5xx → `InternalError`, other → base `AmazonBusinessException`. Each carries `StatusCode` + raw `ResponseBody` and parses Amazon's `{errors:[{code,message,details}]}` shape into a friendly exception message including the request path.
- **`LwaConsentHelper.BuildBusinessAuthorizationUrl(applicationId, redirectUri, state, country)`** — builds the **Amazon-Business-specific** consent URL (`https://www.amazon.<tld>/b2b/abws/oauth?…&applicationId=…`), distinct from the standard LWA `/ap/oa` flow. The Business endpoint takes `applicationId` (the SPP `amzn1.sp.solution.*` value) instead of `client_id` and accepts no `scope` parameter. The previous `BuildAuthorizationUrl` (LWA) is retained for App Center step-5 token exchange and other vanilla LWA flows.
- **`Country.BusinessOAuthUrl`** — derives the regional `/b2b/abws/oauth` host from `AmazonUrl` (e.g. `Country.DE.BusinessOAuthUrl` → `https://www.amazon.de/b2b/abws/oauth`).
- **`appsettings.Local.json` plumbing** — both `SampleCode` and `WebAuthSample` projects now load `appsettings.Local.json` (gitignored) over `appsettings.json`. Committed `appsettings.Local.example.json` templates show the expected shape for new contributors. WebAuthSample's csproj uses `<Content Update>` to mark the file `CopyToPublishDirectory=Never` so secrets never ship in published builds.

### Fixed

- **NSwag deserialization on paginated responses** — `fetch_spec.py` now removes `nextPageToken` / `nextToken` from response-schema `required` arrays during the post-fetch normalization pass. Amazon's specs incorrectly mark these as required, but they're conditionally present (only when there's a next page); NSwag generates strict parsers from `required`, so any single-page response was throwing `ApiException: Could not deserialize`. Affected 7 schema definitions across Reconciliation + Reporting v2025 + Reporting v2021 specs.
- **Cart's parameter-level `Region` enum** name collision — when a hoisted parameter enum had identical values to an existing schema definition, the dedupe pass renames the schema (e.g. `definitions/Region` → `definitions/RegionCode`) so the parameter wins the clean name. NSwag was previously naming the parameter `Region2`.
- **`.github/workflows/dotnet.yml`** — `dotnet restore` / `dotnet build` now pass an explicit solution path (was running with no path from the repo root, which has no project file → `MSBUILD : error MSB1003`). Multi-SDK install (.NET 6 + 8) so the solution's `net8.0` `WebAuthSample` project also builds. Added a Test step. Action versions bumped to `actions/checkout@v4` and `actions/setup-dotnet@v4`.
- **`.github/workflows/nuget.yml`** — same path fix for `dotnet restore`. Lib's csproj path extracted to a workflow `env` var. Bumped to .NET 8 SDK + modern action versions. Added `--no-build` to `dotnet pack` to guarantee the published package matches what was just built.
- **`.gitignore`** — added `appsettings.Local.json` and `appsettings.*.Local.json` patterns. (Also added `.env` patterns and `secrets.json` for completeness.)

### Changed

- **WebAuthSample** — `/connect` and `/appcenter/login-uri` switched from `BuildAuthorizationUrl` (standard LWA, returned `lwa-invalid-parameter-bad-scope` for Amazon Business apps) to `BuildBusinessAuthorizationUrl` (Amazon Business `/b2b/abws/oauth` endpoint). Adds `ApplicationId` to the `AmazonBusiness` config section; removes the unused `Scope` setting.
- **SampleCode** — `Program.cs` is now a runnable smoke-test that exercises every sandbox-supported operation with `try/catch` around each, prints a checked summary table at the end, and uses the documented `x-amzn-api-sandbox.static[].request.parameters` values for each call (so reads return the canned 200 response).

## [0.1.0] — 2026-04-25

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
