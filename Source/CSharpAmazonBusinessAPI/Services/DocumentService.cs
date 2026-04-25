using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.Document;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps the NSwag-generated DocumentApiV1 client (Amazon Business Document API v2021-09-30).
    // Operations are named getReports/createReport/etc. because the underlying transport returns
    // invoice "report documents" — the API itself is the Document API per Amazon's docs.
    public class DocumentService
    {
        private readonly DocumentApiV1 _client;
        private readonly AmazonBusinessCredential _credential;

        public DocumentService(HttpClient httpClient, AmazonBusinessCredential credential)
        {
            _client = new DocumentApiV1(httpClient);
            _credential = credential;
        }

        public DocumentApiV1 Client => _client;

        public Task<GetReportsResponse> GetReportsAsync(
            IEnumerable<string> reportTypes = null,
            IEnumerable<Anonymous> processingStatuses = null,
            IEnumerable<string> marketplaceIds = null,
            int? pageSize = null,
            System.DateTimeOffset? createdSince = null,
            System.DateTimeOffset? createdUntil = null,
            string nextToken = null,
            CancellationToken cancellationToken = default)
        {
            return _client.GetReportsAsync(
                reportTypes, processingStatuses,
                marketplaceIds ?? DefaultMarketplaceIds(),
                pageSize, createdSince, createdUntil, nextToken, cancellationToken);
        }

        public Task<CreateReportResponse> CreateReportAsync(
            CreateReportSpecification body, CancellationToken cancellationToken = default)
        {
            if (body.MarketplaceIds == null || body.MarketplaceIds.Count == 0)
                body.MarketplaceIds = new List<string>(DefaultMarketplaceIds());
            return _client.CreateReportAsync(body, cancellationToken);
        }

        public Task<Report> GetReportAsync(string reportId, CancellationToken cancellationToken = default) =>
            _client.GetReportAsync(reportId, cancellationToken);

        public Task CancelReportAsync(string reportId, CancellationToken cancellationToken = default) =>
            _client.CancelReportAsync(reportId, cancellationToken);

        public Task<ReportDocument> GetReportDocumentAsync(string reportDocumentId, CancellationToken cancellationToken = default) =>
            _client.GetReportDocumentAsync(reportDocumentId, cancellationToken);

        private IEnumerable<string> DefaultMarketplaceIds() =>
            new[] { _credential.MarketPlace.ID };
    }
}
