using System;
using System.Collections.Concurrent;

namespace CSharpAmazonBusinessAPI.Utils
{
    // Translates a Country into whichever NSwag-generated enum a given operation expects.
    // Each spec generates its own Region/ProductRegion enum; the C# member names match
    // Amazon's wire codes (DE/FR/UK/IT/ES/IN/US/CA/MX/JP/AU), so Enum.Parse on the country's
    // AmazonCode finds the matching value.
    //
    // Internal — wrapper services call it; user code never sees the generated enum types.
    internal static class RegionConverter
    {
        // Cache the parsed values per (target enum type, country code) so we're not paying
        // reflection on every call.
        private static readonly ConcurrentDictionary<(Type, string), object> _cache = new ConcurrentDictionary<(Type, string), object>();

        public static T For<T>(Country country) where T : struct, Enum
        {
            if (country == null) throw new ArgumentNullException(nameof(country));
            var code = country.AmazonCode;
            return (T)_cache.GetOrAdd((typeof(T), code), key =>
            {
                if (!Enum.TryParse<T>(key.Item2, ignoreCase: false, out var value))
                    throw new ArgumentException(
                        $"Country '{country.Name}' (code '{code}') has no matching value on {typeof(T).Name}. " +
                        "This API doesn't support that marketplace.", nameof(country));
                return value;
            });
        }
    }
}
