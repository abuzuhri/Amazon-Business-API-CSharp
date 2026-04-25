using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.UserManagement;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps UserManagementApiV1 (Amazon Business User Management API v1).
    public class UserManagementService
    {
        private readonly UserManagementApiV1 _client;

        public UserManagementService(HttpClient httpClient)
        {
            _client = new UserManagementApiV1(httpClient);
        }

        public UserManagementApiV1 Client => _client;

        public Task<CreateBusinessUserAccountResponse> CreateBusinessUserAccountAsync(
            CreateBusinessUserAccountRequest request, CancellationToken cancellationToken = default) =>
            _client.CreateBusinessUserAccountAsync(request, cancellationToken);
    }
}
