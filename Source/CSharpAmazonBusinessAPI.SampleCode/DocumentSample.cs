using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.Document;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class DocumentSample
{
    private readonly AmazonBusinessConnection _connection;

    public DocumentSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public async Task<GetReportsResponse> GetReportsAsync()
    {
        // marketplaceIds defaults to the credential's marketplace when omitted.
        return await _connection.Documents.GetReportsAsync(
            createdSince: DateTime.UtcNow.AddDays(-30));
    }

    public async Task<string> CreateInvoiceReportAsync()
    {
        var spec = new CreateReportSpecification
        {
            ReportType = "GET_FLAT_FILE_VAT_INVOICE_DATA_REPORT",
            DataStartTime = DateTime.UtcNow.AddDays(-30),
            DataEndTime = DateTime.UtcNow,
        };
        var response = await _connection.Documents.CreateReportAsync(spec);
        return response.ReportId;
    }

    public Task<Report> GetReportAsync(string reportId) =>
        _connection.Documents.GetReportAsync(reportId);

    public Task<ReportDocument> GetReportDocumentAsync(string reportDocumentId) =>
        _connection.Documents.GetReportDocumentAsync(reportDocumentId);

    public Task CancelReportAsync(string reportId) =>
        _connection.Documents.CancelReportAsync(reportId);
}
