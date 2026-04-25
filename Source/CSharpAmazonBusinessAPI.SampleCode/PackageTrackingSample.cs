using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.PackageTracking;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class PackageTrackingSample
{
    private readonly AmazonBusinessConnection _connection;

    public PackageTrackingSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<GetPackageTrackingDetailsResponse> GetPackageDetailsAsync(
        string orderId, string shipmentId, string packageId) =>
        _connection.PackageTracking.GetPackageTrackingDetailsAsync(orderId, shipmentId, packageId);
}
