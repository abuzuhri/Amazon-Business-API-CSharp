using Newtonsoft.Json;

namespace CSharpAmazonBusinessAPI.Utils
{
    public class Region
    {
        [JsonConstructor]
        public Region() { }

        private Region(string regionName, string hostUrl, string sandboxHostUrl)
        {
            RegionName = regionName;
            HostUrl = hostUrl;
            SandboxHostUrl = sandboxHostUrl;
        }

        public string RegionName { get; set; }
        public string HostUrl { get; set; }
        public string SandboxHostUrl { get; set; }

        // Production: https://developer-docs.amazon.com/amazon-business/docs/ab-api-endpoints
        // Sandbox:    https://developer-docs.amazon.com/amazon-business/docs/amazon-business-api-sandbox
        public static readonly Region NorthAmerica = new Region("na", "https://na.business-api.amazon.com", "https://sandbox.na.business-api.amazon.com");
        public static readonly Region Europe       = new Region("eu", "https://eu.business-api.amazon.com", "https://sandbox.eu.business-api.amazon.com");
        public static readonly Region FarEast      = new Region("fe", "https://jp.business-api.amazon.com", "https://sandbox.jp.business-api.amazon.com");
    }
}
