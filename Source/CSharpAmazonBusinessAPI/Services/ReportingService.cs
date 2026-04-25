using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.Reporting;
using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps ReportingApiV20250609 (Amazon Business Reporting API v2025-06-09 — current).
    // `country` defaults to the connection's marketplace country.
    public class ReportingService
    {
        private readonly ReportingApiV20250609 _client;
        private readonly AmazonBusinessCredential _credential;

        public ReportingService(HttpClient httpClient, AmazonBusinessCredential credential)
        {
            _client = new ReportingApiV20250609(httpClient);
            _credential = credential;
        }

        public ReportingApiV20250609 Client => _client;

        private Model.Reporting.Region Region(Country c) =>
            RegionConverter.For<Model.Reporting.Region>(c ?? _credential.MarketPlace.Country);

        public Task<GetOrderReportsResponse> GetOrderReportsAsync(
            DateTimeOffset orderStartDate,
            DateTimeOffset orderEndDate,
            Country country = null,
            IEnumerable<string> orderStatuses = null,
            string nextPageToken = null,
            CancellationToken cancellationToken = default) =>
            _client.GetOrderReportsAsync(orderStartDate, orderEndDate, orderStatuses, Region(country), nextPageToken, cancellationToken);

        public Task<GetOrderLineItemReportsResponse> GetOrderLineItemReportsAsync(
            DateTimeOffset orderStartDate,
            DateTimeOffset orderEndDate,
            Country country = null,
            IEnumerable<string> orderIds = null,
            string nextPageToken = null,
            CancellationToken cancellationToken = default) =>
            _client.GetOrderLineItemReportsAsync(orderStartDate, orderEndDate, Region(country), orderIds, nextPageToken, cancellationToken);

        public Task<GetOrderReportsByPurchaseOrderNumberResponse> GetOrderReportsByPurchaseOrderNumberAsync(
            string purchaseOrderNumber,
            Country country = null,
            string nextPageToken = null,
            CancellationToken cancellationToken = default) =>
            _client.GetOrderReportsByPurchaseOrderNumberAsync(purchaseOrderNumber, Region(country), nextPageToken, cancellationToken);

        public Task<GetShipmentReportsResponse> GetShipmentReportsAsync(
            DateTimeOffset orderStartDate,
            DateTimeOffset orderEndDate,
            Country country = null,
            IEnumerable<string> shipmentStatuses = null,
            IEnumerable<string> orderIds = null,
            string nextPageToken = null,
            CancellationToken cancellationToken = default) =>
            _client.GetShipmentReportsAsync(orderStartDate, orderEndDate, shipmentStatuses, orderIds, Region(country), nextPageToken, cancellationToken);

        public Task<GetShipmentLineItemReportsResponse> GetShipmentLineItemReportsAsync(
            DateTimeOffset orderStartDate,
            DateTimeOffset orderEndDate,
            Country country = null,
            IEnumerable<string> orderIds = null,
            IEnumerable<string> purchaseOrderNumbers = null,
            string nextPageToken = null,
            CancellationToken cancellationToken = default) =>
            _client.GetShipmentLineItemReportsAsync(orderStartDate, orderEndDate, orderIds, purchaseOrderNumbers, Region(country), nextPageToken, cancellationToken);
    }
}
