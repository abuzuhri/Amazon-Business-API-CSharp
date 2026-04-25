using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CSharpAmazonBusinessAPI.Utils
{
    public class MarketPlace
    {
        public string ID { get; set; }
        public Region Region { get; set; }
        public Country Country { get; set; }
        public string CurrencyCode { get; set; }

        [JsonConstructor]
        public MarketPlace() { }

        private MarketPlace(string id, Region region, Country country, string currencyCode)
        {
            ID = id;
            Region = region;
            Country = country;
            CurrencyCode = currencyCode;
        }

        public static MarketPlace GetMarketPlaceByID(string id)
        {
            var marketplace = _allMarketplaces.FirstOrDefault(a => a.ID == id);
            if (marketplace == null)
                throw new System.ArgumentException($"InvalidInput, no marketplace registered for ID '{id}'.", nameof(id));
            return marketplace;
        }

        public static MarketPlace GetMarketplaceByCountryCode(string countryCode) =>
            _allMarketplaces.FirstOrDefault(a => a.Country.Code == countryCode);

        // Marketplaces matching the 11 markets supported for Amazon Business developer registration:
        // https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer
        // Marketplace IDs themselves come from https://developer-docs.amazon.com/amazon-business/docs/marketplace-ids

        // North America — https://na.business-api.amazon.com
        public static readonly MarketPlace UnitedStates  = new MarketPlace("ATVPDKIKX0DER",  Region.NorthAmerica, Country.US, "USD");
        public static readonly MarketPlace Canada        = new MarketPlace("A2EUQ1WTGCTBG2", Region.NorthAmerica, Country.CA, "CAD");
        public static readonly MarketPlace Mexico        = new MarketPlace("A1AM78C64UM0Y8", Region.NorthAmerica, Country.MX, "MXN");

        // Europe — https://eu.business-api.amazon.com
        public static readonly MarketPlace UnitedKingdom = new MarketPlace("A1F83G8C2ARO7P", Region.Europe,       Country.GB, "GBP");
        public static readonly MarketPlace Germany       = new MarketPlace("A1PA6795UKMFR9", Region.Europe,       Country.DE, "EUR");
        public static readonly MarketPlace France        = new MarketPlace("A13V1IB3VIYZZH", Region.Europe,       Country.FR, "EUR");
        public static readonly MarketPlace Spain         = new MarketPlace("A1RKKUPIHCS9HS", Region.Europe,       Country.ES, "EUR");
        public static readonly MarketPlace Italy         = new MarketPlace("APJ6JRA9NG5V4",  Region.Europe,       Country.IT, "EUR");
        public static readonly MarketPlace India         = new MarketPlace("A21TJRUUN4KGV",  Region.Europe,       Country.IN, "INR");

        // Far East — https://jp.business-api.amazon.com
        public static readonly MarketPlace Australia     = new MarketPlace("A39IBJ37TRP1C6", Region.FarEast,      Country.AU, "AUD");
        public static readonly MarketPlace Japan         = new MarketPlace("A1VC38T7YXB528", Region.FarEast,      Country.JP, "JPY");

        private static readonly IReadOnlyList<MarketPlace> _allMarketplaces = new[]
        {
            UnitedStates, Canada, Mexico,
            UnitedKingdom, Germany, France, Spain, Italy, India,
            Australia, Japan,
        };
    }
}
