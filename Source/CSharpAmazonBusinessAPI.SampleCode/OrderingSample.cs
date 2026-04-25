using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Ordering;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class OrderingSample
{
    private readonly AmazonBusinessConnection _connection;

    public OrderingSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(IEnumerable<RequestLineItem> lineItems)
    {
        // Trial mode lives on PlaceOrderRequest.OrderRequestProperties — see the generated
        // model. https://developer-docs.amazon.com/amazon-business/docs/validating-an-order-with-trial-mode
        var request = new PlaceOrderRequest
        {
            ExternalId = Guid.NewGuid().ToString(),
            LineItems = lineItems.ToList(),
            // Populate ShippingAddress, PaymentInfo, OrderRequestProperties etc. per the model.
        };
        return await _connection.Ordering.PlaceOrderAsync(request);
    }

    public Task<PlaceOrderResult> GetOrderDetailsAsync(string externalId, string customerEmail) =>
        _connection.Ordering.OrderDetailsAsync(externalId, customerEmail);
}
