using CSharpAmazonBusinessAPI;
using CSharpAmazonBusinessAPI.Model.ProductSearch;

namespace CSharpAmazonBusinessAPI.SampleCode;

public class ProductSearchSample
{
    private readonly AmazonBusinessConnection _connection;

    public ProductSearchSample(AmazonBusinessConnection connection)
    {
        _connection = connection;
    }

    public Task<SearchProductsResult> SearchByKeywordAsync(string keywords, string customerEmail) =>
        _connection.ProductSearch.SearchProductsAsync(
            keywords: keywords,
            customerEmail: customerEmail,
            pageSize: 24,
            sortKey: SortKey.RELEVANCE);

    public Task<ProductsResult> GetProductByAsinAsync(string asin, string customerEmail) =>
        _connection.ProductSearch.GetProductByAsinAsync(asin, customerEmail);
}
