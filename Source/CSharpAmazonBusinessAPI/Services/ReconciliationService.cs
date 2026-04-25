using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.Reconciliation;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps ReconciliationApiV1 (Amazon Business API for Payment Reconciliation v2021-01-08).
    public class ReconciliationService
    {
        private readonly ReconciliationApiV1 _client;

        public ReconciliationService(HttpClient httpClient)
        {
            _client = new ReconciliationApiV1(httpClient);
        }

        public ReconciliationApiV1 Client => _client;

        public Task<TransactionsResponse> GetTransactionsAsync(
            DateTimeOffset feedStartDate, DateTimeOffset feedEndDate, string nextPageToken = null, CancellationToken cancellationToken = default) =>
            _client.GetTransactionsAsync(feedStartDate, feedEndDate, nextPageToken, cancellationToken);

        public Task<GetBatchInvoicePaymentDetailsResponse> GetBatchInvoicePaymentDetailsAsync(
            GetBatchInvoicePaymentDetailsRequest request, CancellationToken cancellationToken = default) =>
            _client.GetBatchInvoicePaymentDetailsAsync(request, cancellationToken);

        public Task<InvoiceDetailsByOrderLineItemsResponse> GetInvoiceDetailsByOrderLineItemsAsync(
            InvoiceDetailsByOrderLineItemsRequest request, CancellationToken cancellationToken = default) =>
            _client.GetInvoiceDetailsByOrderLineItemsAsync(request, cancellationToken);
    }
}
