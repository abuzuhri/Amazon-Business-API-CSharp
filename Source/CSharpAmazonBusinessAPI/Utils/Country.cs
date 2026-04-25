using Newtonsoft.Json;

namespace CSharpAmazonBusinessAPI.Utils
{
    public class Country
    {
        [JsonConstructor]
        public Country() { }

        public string Code { get; set; }
        public string Name { get; set; }
        public string BusinessCentralUrl { get; set; }
        public string AmazonUrl { get; set; }

        // Amazon's API enums use "UK" for the United Kingdom; ISO 3166-1 alpha-2 says "GB".
        // We keep `Code` as the ISO standard and surface `AmazonCode` for wire serialization.
        public string AmazonCode => Code == "GB" ? "UK" : Code;

        private Country(string code, string name, string businessDomain, string amazonUrl)
        {
            Code = code;
            Name = name;
            BusinessCentralUrl = $"https://business.amazon.{businessDomain}";
            AmazonUrl = amazonUrl;
        }

        // The 11 markets supported by Amazon Business developer registration:
        // https://developer-docs.amazon.com/amazon-business/docs/register-as-a-developer

        // North America
        public static readonly Country US = new Country("US", "United States of America", "com",    "https://www.amazon.com/business");
        public static readonly Country CA = new Country("CA", "Canada",                   "ca",     "https://www.amazon.ca/business");
        public static readonly Country MX = new Country("MX", "Mexico",                   "com.mx", "https://www.amazon.com.mx/business");

        // Europe
        public static readonly Country GB = new Country("GB", "United Kingdom", "co.uk", "https://www.amazon.co.uk/business");
        public static readonly Country DE = new Country("DE", "Germany",        "de",    "https://www.amazon.de/business");
        public static readonly Country FR = new Country("FR", "France",         "fr",    "https://www.amazon.fr/business");
        public static readonly Country ES = new Country("ES", "Spain",          "es",    "https://www.amazon.es/business");
        public static readonly Country IT = new Country("IT", "Italy",          "it",    "https://www.amazon.it/business");
        public static readonly Country IN = new Country("IN", "India",          "in",    "https://www.amazon.in/business");

        // Far East
        public static readonly Country AU = new Country("AU", "Australia", "com.au", "https://www.amazon.com.au/business");
        public static readonly Country JP = new Country("JP", "Japan",     "co.jp",  "https://www.amazon.co.jp/business");
    }
}
