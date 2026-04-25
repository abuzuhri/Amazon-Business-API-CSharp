using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.ReportingLegacy;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps ReportingApiV20210108 (Amazon Business Reporting API v2021-01-08 — legacy).
    // The newer v2025-06-09 surface lives on connection.Reporting.
    public class ReportingLegacyService
    {
        private readonly ReportingApiV20210108 _client;

        public ReportingLegacyService(HttpClient httpClient)
        {
            _client = new ReportingApiV20210108(httpClient);
        }

        public ReportingApiV20210108 Client => _client;

        public Task<OrdersOutput> GetOrdersByOrderDateAsync(
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            string nextPageToken = null,
            bool? includeLineItems = null,
            bool? includeShipments = null,
            bool? includeCharges = null,
            CancellationToken cancellationToken = default) =>
            _client.GetOrdersByOrderDateAsync(startDate, nextPageToken, endDate, includeLineItems, includeShipments, includeCharges, cancellationToken);

        public Task<OrdersOutput> GetOrdersByOrderIdAsync(
            string orderId,
            bool? includeLineItems = null,
            bool? includeShipments = null,
            bool? includeCharges = null,
            CancellationToken cancellationToken = default) =>
            _client.GetOrdersByOrderIdAsync(orderId, includeLineItems, includeShipments, includeCharges, cancellationToken);
    }
}
