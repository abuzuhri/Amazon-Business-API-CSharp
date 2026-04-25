using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.ReportingLegacy;

namespace CSharpAmazonBusinessAPI.SampleCode;

// Legacy Reporting API v2021-01-08. Prefer the v2025-06-09 surface (connection.Reporting)
// for new code; this is here for callers still on the old version.
public class ReportingLegacySample
{
    private readonly AmazonBusinessConnection _connection;

    public ReportingLegacySample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<OrdersOutput> GetOrdersLast7DaysAsync() =>
        _connection.ReportingLegacy.GetOrdersByOrderDateAsync(
            startDate: DateTimeOffset.UtcNow.AddDays(-7),
            endDate: DateTimeOffset.UtcNow,
            includeLineItems: true,
            includeShipments: true,
            includeCharges: true);

    public Task<OrdersOutput> GetOrderByIdAsync(string orderId) =>
        _connection.ReportingLegacy.GetOrdersByOrderIdAsync(
            orderId: orderId,
            includeLineItems: true,
            includeShipments: true);
}
