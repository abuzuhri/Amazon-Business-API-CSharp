// Demo of Amazon Business's authorization workflows.
//
// Two flows are implemented side-by-side:
//
//   A) Website authorization workflow (user-initiated)
//      https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow
//
//      GET /                  — landing page, links to either flow
//      GET /connect           — generate CSRF state, redirect to Amazon consent
//      GET /oauth/callback    — verify state, exchange code via LwaConsentHelper
//      GET /test-call         — sample API call with the issued refresh_token
//
//   B) App Center authorization workflow (App Center initiates)
//      https://developer-docs.amazon.com/amazon-business/docs/app-center-authorization-workflow
//
//      GET /appcenter/login-uri    — App Center hits this server-side with
//                                    `amazon_callback_uri` + `amazon_state` (steps 2/3).
//                                    We POST an ack back to amazon_callback_uri then redirect
//                                    the user to Amazon's consent page.
//      GET /appcenter/oauth/callback — Amazon redirects user here after consent (step 4).
//                                    We exchange the code (step 5) and redirect back to
//                                    amazon_callback_uri with completion status (step 6).
//
// Production notes (this demo does NOT do these — see comments below):
//   - Persist refresh_token per-customer in a database (not session / not in-memory cache).
//   - Store ClientSecret in a secret manager, not appsettings.json.
//   - Use real HTTPS in production.
//   - Set Referrer-Policy: no-referrer (we do — Amazon recommends it).

using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Utils;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Country = CSharpAmazonBusinessAPI.Utils.Country;
using MarketPlace = CSharpAmazonBusinessAPI.Utils.MarketPlace;

var builder = WebApplication.CreateBuilder(args);

// Optional local override (gitignored). See appsettings.Local.example.json for the shape.
// Order matters: appsettings.json → appsettings.{Env}.json → appsettings.Local.json
//                → User Secrets (Dev only) → env vars → command line.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".AmazonBusiness.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(10);
});

var app = builder.Build();

app.UseSession();
app.Use(async (ctx, next) =>
{
    // Per Amazon's docs: prevents OAuth params from leaking via Referer to third-party sites.
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

var ab = app.Configuration.GetSection("AmazonBusiness");
string ApplicationId() => ab["ApplicationId"] ?? throw new InvalidOperationException("AmazonBusiness:ApplicationId not set (the amzn1.sp.solution.* value from Solution Provider Portal)");
string ClientId() => ab["ClientId"] ?? throw new InvalidOperationException("AmazonBusiness:ClientId not set");
string ClientSecret() => ab["ClientSecret"] ?? throw new InvalidOperationException("AmazonBusiness:ClientSecret not set");
string RedirectUri() => ab["RedirectUri"] ?? throw new InvalidOperationException("AmazonBusiness:RedirectUri not set");
string AppCenterRedirectUri() => ab["AppCenterRedirectUri"] ?? throw new InvalidOperationException("AmazonBusiness:AppCenterRedirectUri not set");
string MarketplaceId() => ab["MarketPlaceID"] ?? "ATVPDKIKX0DER";
Country MarketplaceCountry() => MarketPlace.GetMarketPlaceByID(MarketplaceId()).Country;

const string StateSessionKey = "lwa_state";
const string RefreshTokenSessionKey = "lwa_refresh_token";

// ===================== Flow A: Website authorization =====================

app.MapGet("/", () => Results.Content(Pages.Home(IsConfigured()), "text/html"));

app.MapGet("/connect", (HttpContext ctx) =>
{
    if (!IsConfigured())
        return Results.Content(Pages.Error("Set AmazonBusiness:ApplicationId / ClientId / ClientSecret / RedirectUri in appsettings.json or User Secrets first."), "text/html");

    var state = GenerateCsrfState();
    ctx.Session.SetString(StateSessionKey, state);

    // Amazon Business uses its own consent endpoint (/b2b/abws/oauth) — NOT the standard
    // LWA endpoint. Pass the SPP `applicationId` (amzn1.sp.solution.*), not the LWA
    // `client_id`. No `scope` parameter — Business consent shows permissions based on the
    // app's registered roles in Solution Provider Portal.
    var url = LwaConsentHelper.BuildBusinessAuthorizationUrl(
        applicationId: ApplicationId(),
        redirectUri:   RedirectUri(),
        state:         state,
        country:       MarketplaceCountry());

    return Results.Redirect(url);
});

app.MapGet("/oauth/callback", async (HttpContext ctx, string? code, string? state, string? error, string? error_description) =>
{
    if (!string.IsNullOrEmpty(error))
        return Results.Content(Pages.Error($"Amazon returned an error: {error}. {error_description}"), "text/html");

    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        return Results.Content(Pages.Error("Missing `code` or `state` on callback URL."), "text/html");

    var expectedState = ctx.Session.GetString(StateSessionKey);
    if (!CsrfStateMatches(state, expectedState))
        return Results.Content(Pages.Error("State mismatch — possible CSRF. Aborting."), "text/html");
    ctx.Session.Remove(StateSessionKey);

    try
    {
        var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(
            code: code,
            clientId: ClientId(),
            clientSecret: ClientSecret(),
            redirectUri: RedirectUri());

        ctx.Session.SetString(RefreshTokenSessionKey, tokens.RefreshToken ?? "");
        return Results.Content(Pages.Success(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresInSeconds), "text/html");
    }
    catch (Exception ex)
    {
        return Results.Content(Pages.Error($"Token exchange failed: {ex.Message}"), "text/html");
    }
});

app.MapGet("/test-call", async (HttpContext ctx) =>
{
    var refreshToken = ctx.Session.GetString(RefreshTokenSessionKey);
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Content(Pages.Error("No refresh token in session. Connect first via the homepage."), "text/html");

    var connection = new AmazonBusinessConnection(new AmazonBusinessCredential
    {
        ClientId = ClientId(),
        ClientSecret = ClientSecret(),
        RefreshToken = refreshToken,
        MarketPlace = MarketPlace.GetMarketPlaceByID(MarketplaceId()),
        Environment = AmazonBusinessCredential.Environments.Sandbox,
    });

    try
    {
        var reports = await connection.Documents.GetReportsAsync(createdSince: DateTime.UtcNow.AddDays(-30));
        return Results.Content(Pages.TestCall(reports.Reports?.Count ?? 0), "text/html");
    }
    catch (Exception ex)
    {
        return Results.Content(Pages.Error($"API call failed: {ex.Message}"), "text/html");
    }
});

// ===================== Flow B: App Center authorization =====================
//
// State bridge between server-side step 2/3 and browser-side step 4: the state we
// generate at step 3 is passed through Amazon's consent flow as `state`, then echoed
// back at step 4. We use it as a key into IMemoryCache to retrieve the original
// `amazon_state` + `amazon_callback_uri` so we can complete step 6.

app.MapGet("/appcenter/login-uri", async (HttpContext ctx, string? amazon_callback_uri, string? amazon_state, IMemoryCache cache) =>
{
    if (!IsConfigured() || string.IsNullOrEmpty(ab["AppCenterRedirectUri"]))
        return Results.Content(Pages.Error("Set AmazonBusiness:AppCenterRedirectUri (in addition to ClientId/ClientSecret) before App Center flow can run."), "text/html");

    if (string.IsNullOrEmpty(amazon_callback_uri) || string.IsNullOrEmpty(amazon_state))
        return Results.Content(Pages.Error("App Center login URI requires amazon_callback_uri + amazon_state query params."), "text/html");

    var state = GenerateCsrfState();

    // Step 3: tell App Center we received the request and redirect back to a fresh state.
    var ackOk = await PostStatusToAppCenter(amazon_callback_uri, AppCenterRedirectUri(), amazon_state, state, "authentication_successful");
    if (!ackOk)
        return Results.Content(Pages.Error($"Failed to acknowledge App Center at {amazon_callback_uri}."), "text/html");

    // Stash the App Center context so the OAuth callback can read it after consent.
    cache.Set(StateCacheKey(state), new AppCenterContext(amazon_state, amazon_callback_uri),
        absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(10));

    var url = LwaConsentHelper.BuildBusinessAuthorizationUrl(
        applicationId: ApplicationId(),
        redirectUri:   AppCenterRedirectUri(),
        state:         state,
        country:       MarketplaceCountry());

    return Results.Redirect(url);
});

app.MapGet("/appcenter/oauth/callback", async (HttpContext ctx, string? code, string? state, string? amazon_callback_uri, string? error, IMemoryCache cache) =>
{
    if (!string.IsNullOrEmpty(error))
        return Results.Content(Pages.Error($"Amazon returned an error: {error}"), "text/html");

    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        return Results.Content(Pages.Error("Missing `code` or `state` on callback URL."), "text/html");

    var contextKey = StateCacheKey(state);
    if (!cache.TryGetValue<AppCenterContext>(contextKey, out var appCenterContext) || appCenterContext is null)
        return Results.Content(Pages.Error("State expired or unknown. Restart the App Center flow."), "text/html");
    cache.Remove(contextKey);

    // Step 4 may include amazon_callback_uri in the query — sanity-check against what we cached.
    var callbackUri = amazon_callback_uri ?? appCenterContext.AmazonCallbackUri;

    string completionStatus;
    string? renderedTokens = null;
    try
    {
        var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(
            code: code,
            clientId: ClientId(),
            clientSecret: ClientSecret(),
            redirectUri: AppCenterRedirectUri());

        ctx.Session.SetString(RefreshTokenSessionKey, tokens.RefreshToken ?? "");
        completionStatus = "auth_code_use_successful";
        renderedTokens = $"refresh_token={Pages.MaskShort(tokens.RefreshToken)}";
    }
    catch (Exception ex)
    {
        // Step 6 (failure): tell App Center the code couldn't be used.
        completionStatus = "auth_code_use_failure";
        renderedTokens = $"error: {ex.Message}";
    }

    // Step 6: redirect the user back to App Center with the outcome.
    var returnUrl = $"{callbackUri}{(callbackUri.Contains('?') ? '&' : '?')}status={Uri.EscapeDataString(completionStatus)}&amazon_state={Uri.EscapeDataString(appCenterContext.AmazonState)}";

    // For the demo, render a confirmation page with a "continue to App Center" link
    // so the developer can see what happened. In production you'd just `Redirect(returnUrl)`.
    return Results.Content(Pages.AppCenterDone(completionStatus, renderedTokens, returnUrl), "text/html");
});

app.Run();

// ===================== Helpers =====================

bool IsConfigured() =>
    !string.IsNullOrEmpty(ab["ApplicationId"]) &&
    !string.IsNullOrEmpty(ab["ClientId"]) &&
    !string.IsNullOrEmpty(ab["ClientSecret"]) &&
    !string.IsNullOrEmpty(ab["RedirectUri"]) &&
    !ab["ClientId"]!.Contains('X') &&
    !ab["ApplicationId"]!.Contains('X');

static string StateCacheKey(string state) => "appcenter_ctx:" + state;

static string GenerateCsrfState()
{
    Span<byte> buf = stackalloc byte[32];
    RandomNumberGenerator.Fill(buf);
    return Convert.ToHexString(buf);
}

static bool CsrfStateMatches(string actual, string? expected)
{
    if (expected is null) return false;
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(actual),
        Encoding.UTF8.GetBytes(expected));
}

// Sends App Center the step-3 ack: form-encoded POST to amazon_callback_uri.
static async Task<bool> PostStatusToAppCenter(
    string amazonCallbackUri,
    string redirectUri,
    string amazonState,
    string state,
    string status)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    var form = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("redirect_uri", redirectUri),
        new KeyValuePair<string, string>("amazon_state", amazonState),
        new KeyValuePair<string, string>("state", state),
        new KeyValuePair<string, string>("status", status),
    });
    try
    {
        using var response = await http.PostAsync(amazonCallbackUri, form);
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

internal record AppCenterContext(string AmazonState, string AmazonCallbackUri);

internal static class Pages
{
    private const string Css = """
        body { font: 14px/1.5 -apple-system, "Segoe UI", system-ui, sans-serif; max-width: 720px; margin: 40px auto; padding: 0 20px; color: #222; }
        h1 { color: #232f3e; }
        h2 { color: #232f3e; margin-top: 32px; }
        .btn { display: inline-block; background: #ff9900; color: #111; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; }
        .btn-secondary { background: #fff; color: #232f3e; border: 1px solid #232f3e; }
        .btn:hover { background: #ffac33; }
        code, pre { background: #f6f8fa; border-radius: 6px; padding: 2px 6px; font-size: 13px; }
        pre { padding: 12px; overflow-x: auto; }
        .error { background: #ffe9e9; border-left: 4px solid #c33; padding: 10px 16px; }
        .ok { background: #e9f7ed; border-left: 4px solid #2c7a3a; padding: 10px 16px; }
        .muted { color: #666; font-size: 13px; }
        """;

    public static string Home(bool configured) => Layout("Amazon Business OAuth demo", $"""
        <h1>Amazon Business — OAuth demos</h1>
        <p>Two consent workflows are implemented side-by-side:</p>

        <h2>A · Website authorization (user-initiated)</h2>
        <p><a href="https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow">Docs.</a> The user clicks <em>Connect</em> on your site → consent on Amazon → callback. Standard OAuth code flow.</p>
        {(configured
            ? "<p><a class=\"btn\" href=\"/connect\">Connect Amazon Business</a></p>"
            : "<div class=\"error\"><strong>Not configured.</strong> Set <code>AmazonBusiness:ClientId</code>, <code>:ClientSecret</code>, and <code>:RedirectUri</code> in <code>appsettings.json</code> or User Secrets.</div>")}

        <h2>B · App Center authorization (App Center initiates)</h2>
        <p><a href="https://developer-docs.amazon.com/amazon-business/docs/app-center-authorization-workflow">Docs.</a> App Center calls your <em>OAuth Login URI</em> server-side first (steps 2-3) with <code>amazon_callback_uri</code> + <code>amazon_state</code>, then drives the user through OAuth, then your callback redirects back to App Center with completion status (step 6).</p>
        <p>Configure these two URIs in App Center for your app, then App Center calls the first URI to start the flow:</p>
        <pre>OAuth Login URI:    /appcenter/login-uri
        OAuth Redirect URI: /appcenter/oauth/callback</pre>
        <p class="muted">There's no button here for this flow — App Center is the entry point. To test locally, simulate App Center by hitting <code>/appcenter/login-uri?amazon_callback_uri=...&amp;amazon_state=...</code>. The status-ack POST in step 3 will fail unless the callback URI is reachable.</p>

        <p class="muted">In production: persist the refresh_token per-customer in a database, store the client_secret in a secret manager, and run behind real HTTPS.</p>
        """);

    public static string Success(string? accessToken, string? refreshToken, int expiresIn) => Layout("Connected", $"""
        <h1>✓ Connected (website flow)</h1>
        <div class="ok">Token exchange successful. Below are the values Amazon returned.</div>
        <h3>access_token</h3>
        <pre>{HttpUtility.HtmlEncode(MaskShort(accessToken))}</pre>
        <p class="muted">Valid for <strong>{expiresIn} seconds</strong> (~1 hour). The SDK refreshes automatically — you don't usually touch this.</p>
        <h3>refresh_token</h3>
        <pre>{HttpUtility.HtmlEncode(MaskShort(refreshToken))}</pre>
        <p class="muted">Long-lived. <strong>Persist this per customer</strong> — pass it to <code>AmazonBusinessCredential.RefreshToken</code> on subsequent calls.</p>
        <p><a class="btn" href="/test-call">Make a test API call</a></p>
        <p><a href="/">← Back</a></p>
        """);

    public static string AppCenterDone(string completionStatus, string? tokenSummary, string returnUrl) => Layout("App Center flow complete", $"""
        <h1>{(completionStatus == "auth_code_use_successful" ? "✓" : "✗")} App Center flow done</h1>
        <div class="{(completionStatus == "auth_code_use_successful" ? "ok" : "error")}">
            Completion status: <code>{HttpUtility.HtmlEncode(completionStatus)}</code><br>
            {HttpUtility.HtmlEncode(tokenSummary)}
        </div>
        <p>In production this would be a 302 redirect to <code>amazon_callback_uri</code> so App Center can finish onboarding the user. The demo renders this confirmation page so you can see what happened:</p>
        <pre>{HttpUtility.HtmlEncode(returnUrl)}</pre>
        <p><a class="btn btn-secondary" href="{HttpUtility.HtmlAttributeEncode(returnUrl)}">Continue to App Center</a></p>
        <p><a href="/">← Back</a></p>
        """);

    public static string TestCall(int reportCount) => Layout("Test call", $"""
        <h1>✓ API call succeeded</h1>
        <div class="ok"><code>connection.Documents.GetReportsAsync</code> against the sandbox returned <strong>{reportCount}</strong> report(s).</div>
        <p>That confirms the refresh_token works end-to-end: the SDK exchanged it for an access_token, attached the bearer header, hit the regional sandbox host, and parsed the response.</p>
        <p><a href="/">← Back</a></p>
        """);

    public static string Error(string message) => Layout("Error", $"""
        <h1>Something went wrong</h1>
        <div class="error">{HttpUtility.HtmlEncode(message)}</div>
        <p><a href="/">← Back</a></p>
        """);

    private static string Layout(string title, string body) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>{HttpUtility.HtmlEncode(title)}</title><style>{Css}</style></head>
        <body>{body}</body>
        </html>
        """;

    public static string MaskShort(string? value) =>
        string.IsNullOrEmpty(value) ? "(empty)"
            : value.Length <= 12 ? "***"
            : value[..6] + "…" + value[^6..];
}
