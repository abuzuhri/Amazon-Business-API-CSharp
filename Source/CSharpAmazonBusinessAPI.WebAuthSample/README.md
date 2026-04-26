# CSharpAmazonBusinessAPI.WebAuthSample

ASP.NET Core (`net8.0`) demo of **both** Amazon Business consent flows, side-by-side:

- **A · Website authorization** — [docs](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow). User-initiated. The user clicks *Connect* on your site, consents on Amazon, gets redirected back. Standard OAuth code flow.
- **B · App Center authorization** — [docs](https://developer-docs.amazon.com/amazon-business/docs/app-center-authorization-workflow). App-Center-initiated. App Center calls your *OAuth Login URI* server-side first, you POST an ack back, then drive the user through OAuth, then your callback redirects back to App Center with completion status.

Both flows share the same LWA token-exchange step (`LwaConsentHelper.ExchangeCodeForTokensAsync`) — the App Center extras are step 2/3 (server-side acknowledgment) and step 6 (return-to-App-Center redirect).

## Routes

| Method | Path | Flow | Purpose |
| --- | --- | --- | --- |
| `GET` | `/` | — | Landing page; links to either flow |
| `GET` | `/connect` | A | Generate state cookie + redirect to Amazon consent |
| `GET` | `/oauth/callback` | A | Verify state, exchange code, render tokens |
| `GET` | `/test-call` | A | Use refresh token to call `Documents.GetReports` against sandbox |
| `GET` | `/appcenter/login-uri` | B | App Center hits this server-side; we ack via POST then redirect user to consent |
| `GET` | `/appcenter/oauth/callback` | B | After user consent, exchange code, redirect back to App Center with completion |

## Setup

### 1. Register your app

Both flows need:

- `ClientId` and `ClientSecret` from the [Solution Provider Portal](https://solutionproviderportal.amazon.com/).
- **For flow A:** add `https://localhost:7271/oauth/callback` to the app's allowed redirect URIs.
- **For flow B:** in App Center, configure:
  - **OAuth Login URI:** `https://localhost:7271/appcenter/login-uri`
  - **OAuth Redirect URI:** `https://localhost:7271/appcenter/oauth/callback`

(Adjust the port if `Properties/launchSettings.json` is changed.)

### 2. Drop in your credentials

From this project folder:

```powershell
dotnet user-secrets set "AmazonBusiness:ClientId"             "amzn1.application-oa2-client.XXXX"
dotnet user-secrets set "AmazonBusiness:ClientSecret"         "XXXX"
dotnet user-secrets set "AmazonBusiness:RedirectUri"          "https://localhost:7271/oauth/callback"
dotnet user-secrets set "AmazonBusiness:AppCenterRedirectUri" "https://localhost:7271/appcenter/oauth/callback"
```

(Or edit `appsettings.json` directly — User Secrets keeps the secret out of the repo.)

### 3. Run it

```powershell
dotnet run
```

Open `https://localhost:7271`.

## Testing each flow

### Flow A (website auth)

Click *Connect Amazon Business* on the homepage → consent on Amazon → land back at `/oauth/callback` → see the masked tokens → optionally hit *Make a test API call*.

### Flow B (App Center)

App Center is the entry point in production — there's no button on the homepage. To test locally without real App Center, you can simulate the step-2 call:

```powershell
# (run a tiny local server to receive the step-3 ack POST and step-6 redirect)
# then hit /appcenter/login-uri with simulated App Center query params:
#
# https://localhost:7271/appcenter/login-uri
#   ?amazon_callback_uri=https://your-fake-app-center.test/echo
#   &amazon_state=fake-app-center-state-token
```

The step-3 POST will fail unless `amazon_callback_uri` is reachable, but the rest of the flow (consent, code exchange, step-6 redirect) will exercise correctly.

## What this demo does NOT do (do these in production)

- **Persist `refresh_token` per customer.** This demo stashes it in the session cookie's server-side store; a real app keys it by your tenant/customer ID in a database.
- **Use a hardened `IMemoryCache`** for the App Center state bridge. The current cache is in-process (single-instance) — for a multi-instance deployment, swap in Redis or a shared store.
- **Step-6 redirect.** The demo renders a confirmation page with a *Continue to App Center* link instead of an immediate redirect, so you can see what happened. In production replace with `Results.Redirect(returnUrl)`.
- **Store `ClientSecret` in a secret manager.** User Secrets is fine for dev; production should pull from Azure Key Vault / AWS Secrets Manager / Vault / etc.
- **Real HTTPS.** Local dev cert is enough for `localhost`. Production needs a real certificate.

## How the OAuth dance maps to code

Both flows ultimately call:

```csharp
// Step 5 in either flow:
var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(
    code, clientId, clientSecret, redirectUri);
```

The differences are around it:

| Step | Website auth | App Center auth |
| --- | --- | --- |
| Generate consent URL | `LwaConsentHelper.BuildAuthorizationUrl` (in `/connect`) | Same helper, called inside `/appcenter/login-uri` after acknowledging App Center |
| State storage | session cookie | `IMemoryCache` keyed by state (because step 2 is server-side and step 4 is browser-side, no shared session) |
| After token exchange | render tokens | redirect back to `amazon_callback_uri` with `status=auth_code_use_successful` |
