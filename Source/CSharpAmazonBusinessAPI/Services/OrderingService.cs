using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.Ordering;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps OrderingApiV1 (Amazon Business Ordering API v2022-10-30).
    public class OrderingService
    {
        private readonly OrderingApiV1 _client;

        public OrderingService(HttpClient httpClient)
        {
            _client = new OrderingApiV1(httpClient);
        }

        public OrderingApiV1 Client => _client;

        public Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default) =>
            _client.PlaceOrderAsync(request, cancellationToken);

        public Task<PlaceOrderResult> OrderDetailsAsync(string externalId, string customerEmail, CancellationToken cancellationToken = default) =>
            _client.OrderDetailsAsync(externalId, customerEmail, cancellationToken);
    }
}
