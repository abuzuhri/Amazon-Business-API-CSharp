using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.ApplicationManagement;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps the NSwag-generated ApplicationManagementApiV1 client (Application Management API v2023-11-30).
    // Sole operation: trigger an out-of-band rotation of the LWA client secret. Amazon delivers
    // the new secret to the developer-registered SQS queue — this call only signals the rotation.
    // After receiving the new secret, callers should pass it to AmazonBusinessCredential.RotateClientSecret.
    public class ApplicationManagementService
    {
        private readonly ApplicationManagementApiV1 _client;

        public ApplicationManagementService(HttpClient httpClient)
        {
            _client = new ApplicationManagementApiV1(httpClient);
        }

        public ApplicationManagementApiV1 Client => _client;

        public Task RotateApplicationClientSecretAsync(CancellationToken cancellationToken = default) =>
            _client.RotateApplicationClientSecretAsync(cancellationToken);
    }
}
