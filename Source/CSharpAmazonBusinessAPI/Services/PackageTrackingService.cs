using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.PackageTracking;
using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps PackageTrackingApiV1 (Amazon Business API for Package Tracking Details v2025-07-02).
    public class PackageTrackingService
    {
        private readonly PackageTrackingApiV1 _client;
        private readonly AmazonBusinessCredential _credential;

        public PackageTrackingService(HttpClient httpClient, AmazonBusinessCredential credential)
        {
            _client = new PackageTrackingApiV1(httpClient);
            _credential = credential;
        }

        public PackageTrackingApiV1 Client => _client;

        public Task<GetPackageTrackingDetailsResponse> GetPackageTrackingDetailsAsync(
            string orderId,
            string shipmentId,
            string packageId,
            Country country = null,
            string locale = "en-US",
            CancellationToken cancellationToken = default) =>
            _client.GetPackageTrackingDetailsAsync(
                orderId, shipmentId, packageId, locale,
                RegionConverter.For<Model.PackageTracking.Region>(country ?? _credential.MarketPlace.Country),
                cancellationToken);
    }
}
