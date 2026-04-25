using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpAmazonBusinessAPI.Model.ProductSearch;
using CSharpAmazonBusinessAPI.Utils;

namespace CSharpAmazonBusinessAPI.Services
{
    // Wraps ProductSearchApiV1 (Amazon Business API for Products v2020-08-26).
    //
    // Two convenience methods (keyword search + product-by-ASIN) are exposed; the rest stay on
    // .Client for full IntelliSense over the wide raw surface. `country` defaults to the
    // connection's marketplace country.
    public class ProductSearchService
    {
        private readonly ProductSearchApiV1 _client;
        private readonly AmazonBusinessCredential _credential;

        public ProductSearchService(HttpClient httpClient, AmazonBusinessCredential credential)
        {
            _client = new ProductSearchApiV1(httpClient);
            _credential = credential;
        }

        public ProductSearchApiV1 Client => _client;

        private ProductRegion ProductRegion(Country c) =>
            RegionConverter.For<ProductRegion>(c ?? _credential.MarketPlace.Country);

        public Task<SearchProductsResult> SearchProductsAsync(
            string keywords,
            string customerEmail,
            Country country = null,
            int? pageNumber = null,
            int? pageSize = null,
            SortKey? sortKey = null,
            string locale = "en_US",
            CancellationToken cancellationToken = default) =>
            _client.SearchProductsRequestAsync(
                keywords: keywords,
                productRegion: ProductRegion(country),
                shippingRegion: null, locale: locale, shippingPostalCode: null,
                facets: null, pageNumber: pageNumber, pageSize: pageSize,
                groupTag: null, category: null, subCategory: null,
                availability: null, deliveryDay: null,
                eligibleForFreeShipping: null, primeEligible: null,
                upc: null, isbn: null, sku: null, ean: null,
                partNumber: null, oemPartNumber: null,
                searchRefinements: null, productFilters: null,
                x_amz_user_email: customerEmail,
                inclusionsForProducts: null, inclusionsForOffers: null,
                sortKey: sortKey, minPrice: null, maxPrice: null,
                cancellationToken);

        public Task<ProductsResult> GetProductByAsinAsync(
            string asin,
            string customerEmail,
            Country country = null,
            int quantity = 1,
            string locale = "en_US",
            CancellationToken cancellationToken = default) =>
            _client.ProductsRequestAsync(
                productId: asin,
                productRegion: ProductRegion(country),
                shippingRegion: null, locale: locale, shippingPostalCode: null,
                quantity: quantity, facets: null,
                x_amz_user_email: customerEmail,
                inclusionsForProducts: null, inclusionsForOffers: null,
                groupTag: null,
                cancellationToken);
    }
}
