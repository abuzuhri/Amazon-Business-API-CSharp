using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CSharpAmazonBusinessAPI.Authentication
{
    public class DebugLogHandler : DelegatingHandler
    {
        private static readonly HashSet<string> SensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            LwaAuthHandler.AccessTokenHeader,
            "Authorization",
            "x-amz-security-token",
        };

        private readonly AmazonBusinessCredential _credential;
        private readonly ILogger _logger;

        public DebugLogHandler(AmazonBusinessCredential credential, ILogger logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_credential.IsDebugMode)
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("---- [Amazon Business DEBUG] Request ----");
            sb.AppendLine($"  {request.Method} {request.RequestUri}");
            AppendHeaders(sb, request.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
            if (request.Content != null)
            {
                AppendHeaders(sb, request.Content.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
                var requestBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                sb.AppendLine("  Body:");
                sb.AppendLine(PrettyOrRaw(requestBody));
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            sb.AppendLine("---- [Amazon Business DEBUG] Response ----");
            sb.AppendLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
            AppendHeaders(sb, response.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
            if (response.Content != null)
            {
                AppendHeaders(sb, response.Content.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                sb.AppendLine("  Body:");
                sb.AppendLine(PrettyOrRaw(responseBody));
            }
            sb.AppendLine("---- [Amazon Business DEBUG] End ----");

            var output = sb.ToString();
            if (_logger != null) _logger.LogInformation(output);
            else Console.WriteLine(output);

            return response;
        }

        private static void AppendHeaders(StringBuilder sb, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                var value = string.Join(", ", header.Value);
                if (SensitiveHeaders.Contains(header.Key))
                    value = Mask(value);
                sb.AppendLine($"  {header.Key}: {value}");
            }
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= 8 ? "***" : value.Substring(0, 4) + "***" + value.Substring(value.Length - 4);
        }

        private static string PrettyOrRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "    (empty)";
            try
            {
                var obj = JsonConvert.DeserializeObject(raw);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return raw;
            }
        }
    }
}
