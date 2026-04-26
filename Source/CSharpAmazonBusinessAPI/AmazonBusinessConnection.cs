using System;
using System.Net.Http;
using CSharpAmazonBusinessAPI.Authentication;
using CSharpAmazonBusinessAPI.Exceptions;
using CSharpAmazonBusinessAPI.Services;
using CSharpAmazonBusinessAPI.Utils;
using Microsoft.Extensions.Logging;
using static CSharpAmazonBusinessAPI.AmazonBusinessCredential;

namespace CSharpAmazonBusinessAPI
{
    public class AmazonBusinessConnection
    {
        public AmazonBusinessCredential Credentials { get; }
        public DocumentService Documents { get; }
        public CartService Cart { get; }
        public ApplicationManagementService Applications { get; }
        public OrderingService Ordering { get; }
        public PackageTrackingService PackageTracking { get; }
        public ProductSearchService ProductSearch { get; }
        public ReconciliationService Reconciliation { get; }
        public ReportingService Reporting { get; }
        public ReportingLegacyService ReportingLegacy { get; }
        public UserManagementService Users { get; }

        public AmazonBusinessConnection(AmazonBusinessCredential credential, ILoggerFactory loggerFactory = null)
        {
            ValidateAndNormalize(credential);
            Credentials = credential;

            var httpClient = BuildHttpClient(credential, loggerFactory);
            Documents = new DocumentService(httpClient, credential);
            Cart = new CartService(httpClient, credential);
            Applications = new ApplicationManagementService(httpClient);
            Ordering = new OrderingService(httpClient);
            PackageTracking = new PackageTrackingService(httpClient, credential);
            ProductSearch = new ProductSearchService(httpClient, credential);
            Reconciliation = new ReconciliationService(httpClient);
            Reporting = new ReportingService(httpClient, credential);
            ReportingLegacy = new ReportingLegacyService(httpClient);
            Users = new UserManagementService(httpClient);
        }

        public MarketPlace CurrentMarketPlace => Credentials.MarketPlace;

        // Handler chain (outermost → innermost):
        //   ErrorTranslation → RateLimit → Auth → CsvRewrite → Debug → HttpClientHandler
        // - ErrorTranslation maps non-2xx into our AmazonBusinessException hierarchy *before*
        //   NSwag-generated clients see the response, so callers never see ApiException<ErrorList>.
        // - RateLimit retries on 429; on retry, Auth re-attaches a fresh token if needed.
        // - Auth adds the LWA bearer; on 401, invalidates the cache and retries once.
        // - CsvRewrite joins repeated query keys (NSwag emits multi-format, Amazon expects csv).
        // - Debug sees the final wire-format request (with auth header + csv arrays) and response.
        private static HttpClient BuildHttpClient(AmazonBusinessCredential credential, ILoggerFactory loggerFactory)
        {
            var transport = new HttpClientHandler();
            if (credential.Proxy != null)
            {
                transport.Proxy = credential.Proxy;
                transport.UseProxy = true;
            }

            var debug = new DebugLogHandler(credential, loggerFactory?.CreateLogger<AmazonBusinessConnection>(), transport);
            var csv = new CsvArrayRewriteHandler(debug);
            var isoDate = new IsoDateTimeRewriteHandler(csv);
            var lwa = new LwaAuthHandler(credential, new LwaClient(credential.Proxy), isoDate);
            var rateLimit = new RateLimitHandler(credential, lwa);
            var errors = new ErrorTranslationHandler(rateLimit);

            var baseUrl = credential.Environment == Environments.Sandbox
                ? credential.MarketPlace.Region.SandboxHostUrl
                : credential.MarketPlace.Region.HostUrl;

            return new HttpClient(errors)
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            };
        }

        private static void ValidateAndNormalize(AmazonBusinessCredential credential)
        {
            if (credential == null)
                throw new AmazonBusinessUnauthorizedException(
                    "Cannot create AmazonBusinessConnection without credentials.",
                    System.Net.HttpStatusCode.Unauthorized, null);

            if (string.IsNullOrEmpty(credential.ClientId))
                throw new AmazonBusinessInvalidInputException("ClientId cannot be empty.");
            if (string.IsNullOrEmpty(credential.ClientSecret))
                throw new AmazonBusinessInvalidInputException("ClientSecret cannot be empty.");
            if (string.IsNullOrEmpty(credential.RefreshToken))
                throw new AmazonBusinessInvalidInputException("RefreshToken cannot be empty.");

            if (credential.MarketPlace == null)
            {
                if (string.IsNullOrEmpty(credential.MarketPlaceID))
                    throw new AmazonBusinessInvalidInputException(
                        "Either MarketPlace or MarketPlaceID must be set.");
                credential.MarketPlace = MarketPlace.GetMarketPlaceByID(credential.MarketPlaceID);
            }
        }
    }
}
