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
        // The static sandbox does EXACT (not subset) parameter matching, so any extra
        // query parameter — even a defaulted-from-credential marketplaceIds — breaks the
        // match and returns 400 "Could not match input arguments". Pass an empty array
        // explicitly to skip the wrapper's marketplaceIds-from-credential default.
        // Documented sandbox pattern:
        //   https://developer-docs.amazon.com/amazon-business/docs/document-api-static-sandbox-guide
        // Production: drop the explicit empty array and pass the real filters you want.
        return await _connection.Documents.GetReportsAsync(
            reportTypes:        new[] { "FEE_DISCOUNTS_REPORT", "GET_AFN_INVENTORY_DATA" },
            processingStatuses: new[] { Anonymous.IN_QUEUE, Anonymous.IN_PROGRESS },
            marketplaceIds:     Array.Empty<string>());
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
