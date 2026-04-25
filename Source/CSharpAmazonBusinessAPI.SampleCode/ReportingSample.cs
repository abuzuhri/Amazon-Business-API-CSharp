using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Reporting;

namespace CSharpAmazonBusinessAPI.SampleCode;

// Reporting v2025-06-09. `country` defaults to the connection's marketplace.
public class ReportingSample
{
    private readonly AmazonBusinessConnection _connection;

    public ReportingSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<GetOrderReportsResponse> GetOrderReportsLast7DaysAsync() =>
        _connection.Reporting.GetOrderReportsAsync(
            orderStartDate: DateTimeOffset.UtcNow.AddDays(-7),
            orderEndDate: DateTimeOffset.UtcNow);

    public Task<GetShipmentReportsResponse> GetShipmentReportsAsync(IEnumerable<string> orderIds) =>
        _connection.Reporting.GetShipmentReportsAsync(
            orderStartDate: DateTimeOffset.UtcNow.AddDays(-30),
            orderEndDate: DateTimeOffset.UtcNow,
            orderIds: orderIds);

    public Task<GetOrderReportsByPurchaseOrderNumberResponse> GetByPurchaseOrderAsync(string poNumber) =>
        _connection.Reporting.GetOrderReportsByPurchaseOrderNumberAsync(purchaseOrderNumber: poNumber);
}
