using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.Cart;
using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps the NSwag-generated CartApiV1 client (Amazon Business Cart API v2025-04-30).
    // `country` defaults to the connection's marketplace country — pass an explicit Country
    // only if you need to override per-call. Generated Region enum stays internal.
    public class CartService
    {
        private readonly CartApiV1 _client;
        private readonly AmazonBusinessCredential _credential;

        public CartService(HttpClient httpClient, AmazonBusinessCredential credential)
        {
            _client = new CartApiV1(httpClient);
            _credential = credential;
        }

        public CartApiV1 Client => _client;

        private Country DefaultCountry(Country country) => country ?? _credential.MarketPlace.Country;

        public Task<CartDetailsResult> ListCartsAsync(
            string customerEmail,
            Country country = null,
            string pageToken = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default) =>
            _client.ListCartsAsync(customerEmail, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), pageToken, pageSize, cancellationToken);

        public Task<Cart> GetCartAsync(string cartId, Country country = null, CancellationToken cancellationToken = default) =>
            _client.GetCartAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), cancellationToken);

        public Task<CartItems> GetItemsAsync(string cartId, Country country = null, CancellationToken cancellationToken = default) =>
            _client.GetItemsAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), cancellationToken);

        public Task<AddItemsResult> AddItemsAsync(
            string cartId, AddItemsRequest request, Country country = null, CancellationToken cancellationToken = default) =>
            _client.AddItemsAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), request, cancellationToken);

        public Task<ModifyItemsResult> ModifyItemsAsync(
            string cartId, ModifyItemsRequest request, Country country = null, CancellationToken cancellationToken = default) =>
            _client.ModifyItemsAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), request, cancellationToken);

        public Task DeleteItemsAsync(string cartId, Country country = null, CancellationToken cancellationToken = default) =>
            _client.DeleteItemsAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), cancellationToken);

        public Task<EstimatedTotalPurchaseCostResult> GetEstimatedTotalPurchaseCostAsync(
            string cartId, EstimatedTotalPurchaseCostRequest request, Country country = null, CancellationToken cancellationToken = default) =>
            _client.GetEstimatedTotalPurchaseCostAsync(cartId, RegionConverter.For<Model.Cart.Region>(DefaultCountry(country)), request, cancellationToken);
    }
}
