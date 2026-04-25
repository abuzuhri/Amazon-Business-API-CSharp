// Demo of Amazon Business's website-authorization workflow:
// https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow
//
// What this app does:
//   1. GET /            — landing page with a "Connect Amazon Business" button.
//   2. GET /connect     — generates a CSRF state token, stores it in a session cookie,
//                         and redirects the customer's browser to Amazon's consent URL.
//   3. GET /oauth/callback
//                       — receives `code` + `state` from Amazon, verifies state matches
//                         the session cookie (CSRF guard), exchanges the code for a
//                         refresh_token via LwaConsentHelper, and renders the result.
//   4. GET /test-call   — uses the just-issued refresh_token to make a real Documents.GetReports
//                         call against the sandbox, proving the credentials work end-to-end.
//
// Production notes (this demo does NOT do these — see comments below):
//   - Persist the refresh_token per-customer (database, not session).
//   - Store the LWA client_secret in a secret manager, never in appsettings.json.
//   - Use HTTPS in production with a real certificate.
//   - Set Referrer-Policy: no-referrer (we do — Amazon recommends it).

using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
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
    // Amazon's website-authorization workflow recommends this header so OAuth params
    // can't leak via Referer when the page links to third-party sites.
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

var ab = app.Configuration.GetSection("AmazonBusiness");
string ClientId() => ab["ClientId"] ?? throw new InvalidOperationException("AmazonBusiness:ClientId not set");
string ClientSecret() => ab["ClientSecret"] ?? throw new InvalidOperationException("AmazonBusiness:ClientSecret not set");
string RedirectUri() => ab["RedirectUri"] ?? throw new InvalidOperationException("AmazonBusiness:RedirectUri not set");
string MarketplaceId() => ab["MarketPlaceID"] ?? "ATVPDKIKX0DER";
string Scope() => ab["Scope"] ?? "profile";

const string StateSessionKey = "lwa_state";
const string RefreshTokenSessionKey = "lwa_refresh_token";

// ------------ Routes ------------

app.MapGet("/", () => Results.Content(Pages.Home(IsConfigured()), "text/html"));

app.MapGet("/connect", (HttpContext ctx) =>
{
    if (!IsConfigured())
        return Results.Content(Pages.Error("Set AmazonBusiness:ClientId / ClientSecret / RedirectUri in appsettings.json or User Secrets first."), "text/html");

    var state = GenerateCsrfState();
    ctx.Session.SetString(StateSessionKey, state);

    var url = LwaConsentHelper.BuildAuthorizationUrl(
        clientId: ClientId(),
        redirectUri: RedirectUri(),
        state: state,
        scope: Scope());

    return Results.Redirect(url);
});

app.MapGet("/oauth/callback", async (HttpContext ctx, string? code, string? state, string? error, string? error_description) =>
{
    if (!string.IsNullOrEmpty(error))
        return Results.Content(Pages.Error($"Amazon returned an error: {error}. {error_description}"), "text/html");

    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        return Results.Content(Pages.Error("Missing `code` or `state` on callback URL."), "text/html");

    var expectedState = ctx.Session.GetString(StateSessionKey);
    if (expectedState is null || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(expectedState)))
    {
        return Results.Content(Pages.Error("State mismatch — possible CSRF. Aborting."), "text/html");
    }
    ctx.Session.Remove(StateSessionKey);

    try
    {
        var tokens = await LwaConsentHelper.ExchangeCodeForTokensAsync(
            code: code,
            clientId: ClientId(),
            clientSecret: ClientSecret(),
            redirectUri: RedirectUri());

        // In production: persist tokens.RefreshToken to your customer database keyed by
        // whoever just authenticated. Here we stash it in the session for the demo.
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
        var count = reports.Reports?.Count ?? 0;
        return Results.Content(Pages.TestCall(count), "text/html");
    }
    catch (Exception ex)
    {
        return Results.Content(Pages.Error($"API call failed: {ex.Message}"), "text/html");
    }
});

app.Run();

// ------------ Helpers ------------

bool IsConfigured() =>
    !string.IsNullOrEmpty(ab["ClientId"]) &&
    !string.IsNullOrEmpty(ab["ClientSecret"]) &&
    !string.IsNullOrEmpty(ab["RedirectUri"]) &&
    !ab["ClientId"]!.Contains('X');  // catch the placeholder

static string GenerateCsrfState()
{
    Span<byte> buf = stackalloc byte[32];
    RandomNumberGenerator.Fill(buf);
    return Convert.ToHexString(buf);
}

static class Pages
{
    private const string Css = """
        body { font: 14px/1.5 -apple-system, "Segoe UI", system-ui, sans-serif; max-width: 720px; margin: 40px auto; padding: 0 20px; color: #222; }
        h1 { color: #232f3e; }
        .btn { display: inline-block; background: #ff9900; color: #111; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; }
        .btn:hover { background: #ffac33; }
        code, pre { background: #f6f8fa; border-radius: 6px; padding: 2px 6px; font-size: 13px; }
        pre { padding: 12px; overflow-x: auto; }
        .error { background: #ffe9e9; border-left: 4px solid #c33; padding: 10px 16px; }
        .ok { background: #e9f7ed; border-left: 4px solid #2c7a3a; padding: 10px 16px; }
        .muted { color: #666; font-size: 13px; }
        """;

    public static string Home(bool configured) => Layout("Amazon Business OAuth demo", $"""
        <h1>Amazon Business — website authorization demo</h1>
        <p>Walks through the OAuth-style consent flow described in the
        <a href="https://developer-docs.amazon.com/amazon-business/docs/website-authorization-workflow">website-authorization workflow</a>:</p>
        <ol>
          <li>Click <em>Connect Amazon Business</em> — we generate a CSRF state cookie and redirect you to Amazon's consent page.</li>
          <li>Amazon redirects back here with a one-time <code>code</code> + the same <code>state</code>.</li>
          <li>We verify the state, exchange the code for a long-lived <code>refresh_token</code>, and render the result.</li>
        </ol>
        {(configured
            ? "<p><a class=\"btn\" href=\"/connect\">Connect Amazon Business</a></p>"
            : "<div class=\"error\"><strong>Not configured.</strong> Set <code>AmazonBusiness:ClientId</code>, <code>:ClientSecret</code>, and <code>:RedirectUri</code> in <code>appsettings.json</code> or User Secrets, then restart.</div>")}
        <p class="muted">In production: persist the refresh_token per-customer in a database, store the client_secret in a secret manager, and run behind real HTTPS.</p>
        """);

    public static string Success(string? accessToken, string? refreshToken, int expiresIn) => Layout("Connected", $"""
        <h1>✓ Connected</h1>
        <div class="ok">Token exchange successful. Below are the values Amazon returned.</div>
        <h3>access_token</h3>
        <pre>{HttpUtility.HtmlEncode(Mask(accessToken))}</pre>
        <p class="muted">Valid for <strong>{expiresIn} seconds</strong> (~1 hour). The SDK refreshes automatically — you don't usually touch this.</p>
        <h3>refresh_token</h3>
        <pre>{HttpUtility.HtmlEncode(Mask(refreshToken))}</pre>
        <p class="muted">Long-lived. <strong>Persist this per customer</strong> — pass it to <code>AmazonBusinessCredential.RefreshToken</code> on subsequent calls.</p>
        <p><a class="btn" href="/test-call">Make a test API call</a></p>
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

    private static string Mask(string? value) =>
        string.IsNullOrEmpty(value) ? "(empty)"
            : value.Length <= 12 ? "***"
            : value[..6] + "…" + value[^6..];
}
