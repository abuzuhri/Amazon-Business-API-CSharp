using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Cart;

namespace CSharpAmazonBusinessAPI.SampleCode;

// `country` defaults to the connection's marketplace country — pass it explicitly only when
// you need to call against a different marketplace from one connection.
public class CartSample
{
    private readonly AmazonBusinessConnection _connection;

    public CartSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<CartDetailsResult> ListCartsAsync(string customerEmail) =>
        _connection.Cart.ListCartsAsync(customerEmail, pageSize: 25);

    public Task<Cart> GetCartAsync(string cartId) =>
        _connection.Cart.GetCartAsync(cartId);

    public Task<CartItems> GetItemsAsync(string cartId) =>
        _connection.Cart.GetItemsAsync(cartId);

    public Task<AddItemsResult> AddItemAsync(string cartId, string asin, int quantity)
    {
        var request = new AddItemsRequest
        {
            Items = new List<AddItemRequest>
            {
                new AddItemRequest { ProductIdentifier = asin, Quantity = quantity },
            },
        };
        return _connection.Cart.AddItemsAsync(cartId, request);
    }

    public Task<EstimatedTotalPurchaseCostResult> EstimateCostAsync(string cartId, Address shippingAddress)
    {
        var request = new EstimatedTotalPurchaseCostRequest { Address = shippingAddress };
        return _connection.Cart.GetEstimatedTotalPurchaseCostAsync(cartId, request);
    }
}
