# CSharpAmazonBusinessAPI.WebAuthSample

ASP.NET Core (`net8.0`) demo of Amazon Business's [website authorization workflow](https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow). Run locally to walk through:

1. Generate a CSRF state token, store it in a session cookie
2. Redirect the customer's browser to Amazon's consent page
3. Receive Amazon's callback with `code` + `state`
4. Verify state, exchange code via `LwaConsentHelper.ExchangeCodeForTokensAsync`
5. Render `access_token` + `refresh_token` (masked); offer to make a test API call against the sandbox

## Setup

1. **Register your app** in the [Solution Provider Portal](https://solutionproviderportal.amazon.com/) and add `https://localhost:7271/oauth/callback` to the app's allowed redirect URIs. (Adjust the port if `Properties/launchSettings.json` is changed.)

2. **Drop in your credentials.** From this project folder:

   ```powershell
   dotnet user-secrets set "AmazonBusiness:ClientId"     "amzn1.application-oa2-client.XXXX"
   dotnet user-secrets set "AmazonBusiness:ClientSecret" "XXXX"
   dotnet user-secrets set "AmazonBusiness:RedirectUri"  "https://localhost:7271/oauth/callback"
   ```

   (You can also edit `appsettings.json` directly, but User Secrets keeps the secret out of the repo.)

3. **Run it.**

   ```powershell
   dotnet run
   ```

   Open `https://localhost:7271`, click **Connect Amazon Business**, complete consent on Amazon's page, and you'll land back here with the issued tokens.

4. **Test the tokens** — click *Make a test API call* on the success page. It uses the just-issued `refresh_token` to call `connection.Documents.GetReportsAsync` against the sandbox.

## What the demo does NOT do (do these in production)

- **Persist `refresh_token` per customer.** This demo stashes it in the session cookie's server-side store; a real app keys it by your tenant/customer ID in a database.
- **Store `ClientSecret` in a secret manager.** User Secrets is fine for dev; production should pull from Azure Key Vault / AWS Secrets Manager / Vault / etc.
- **Real HTTPS.** Local dev cert is enough for `localhost`. Production needs a real certificate.

## How the code is laid out

- `Program.cs` — minimal-API setup, all 4 routes (`/`, `/connect`, `/oauth/callback`, `/test-call`), and inline HTML templates. No Razor / no MVC — kept dependency-light to make the OAuth dance easy to read.
- `appsettings.json` — placeholder credentials (overridden by User Secrets in dev).
- `Properties/launchSettings.json` — `dotnet run` listens on `https://localhost:7271`.

The actual OAuth logic is two calls:

```csharp
// /connect
var url = LwaConsentHelper.BuildAuthorizationUrl(clientId, redirectUri, state, scope);
return Results.Redirect(url);

// /oauth/callback
var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(code, clientId, clientSecret, redirectUri);
```

Everything else (state cookie, `Referrer-Policy: no-referrer` middleware, error rendering, masked-token display) is web-app plumbing around those two calls.
