using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Reconciliation;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class ReconciliationSample
{
    private readonly AmazonBusinessConnection _connection;

    public ReconciliationSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<TransactionsResponse> GetTransactionsLast30DaysAsync() =>
        _connection.Reconciliation.GetTransactionsAsync(
            feedStartDate: DateTimeOffset.UtcNow.AddDays(-30),
            feedEndDate: DateTimeOffset.UtcNow);

    public Task<GetBatchInvoicePaymentDetailsResponse> GetBatchInvoiceDetailsAsync(IEnumerable<string> invoiceIds)
    {
        var request = new GetBatchInvoicePaymentDetailsRequest
        {
            InvoiceIds = invoiceIds.ToList(),
        };
        return _connection.Reconciliation.GetBatchInvoicePaymentDetailsAsync(request);
    }
}
