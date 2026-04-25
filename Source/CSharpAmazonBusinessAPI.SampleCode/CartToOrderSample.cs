using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Cart;
using CSharpAmazonBusinessAPI.Model.Ordering;

namespace CSharpAmazonBusinessAPI.SampleCode;

// End-to-end REST workflow: search → add to cart → place order. Country defaults from the
// connection's marketplace; pass it explicitly only to override per call.
public class CartToOrderSample
{
    private readonly AmazonBusinessConnection _connection;

    public CartToOrderSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public async Task<PlaceOrderResult?> SearchAddAndOrderAsync(
        string customerEmail,
        string keywords,
        string cartId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var search = await _connection.ProductSearch.SearchProductsAsync(
            keywords: keywords,
            customerEmail: customerEmail,
            pageSize: 1,
            cancellationToken: cancellationToken);

        var firstAsin = search.Products?.FirstOrDefault()?.Asin;
        if (string.IsNullOrEmpty(firstAsin)) return null;

        await _connection.Cart.AddItemsAsync(
            cartId,
            new AddItemsRequest
            {
                Items = new List<AddItemRequest>
                {
                    new AddItemRequest { ProductIdentifier = firstAsin, Quantity = quantity },
                },
            },
            cancellationToken: cancellationToken);

        var order = new PlaceOrderRequest
        {
            ExternalId = Guid.NewGuid().ToString(),
            // LineItems, ShippingAddress, PaymentInfo, OrderRequestProperties etc. go here.
        };
        return await _connection.Ordering.PlaceOrderAsync(order, cancellationToken);
    }
}
