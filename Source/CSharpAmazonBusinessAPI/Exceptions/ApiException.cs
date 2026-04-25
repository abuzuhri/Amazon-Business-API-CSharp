using System;
using System.Collections.Generic;

namespace CSharpAmazonBusinessAPI.Exceptions
{
    // Shape matches what NSwag emits when GenerateExceptionClasses is on. Lives here (rather
    // than per-API model namespaces) so multiple generated clients can share one type via the
    // AdditionalNamespaceUsages csproj option. Where a generated client emits its own ApiException
    // alongside (e.g. DocumentApiV1), C# scope resolution prefers the local one — no ambiguity.
    public partial class ApiException : Exception
    {
        public int StatusCode { get; private set; }
        public string Response { get; private set; }
        public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        public ApiException(
            string message,
            int statusCode,
            string response,
            IReadOnlyDictionary<string, IEnumerable<string>> headers,
            Exception innerException)
            : base(BuildMessage(message, statusCode, response), innerException)
        {
            StatusCode = statusCode;
            Response = response;
            Headers = headers;
        }

        public override string ToString() =>
            $"HTTP Response: \n\n{Response}\n\n{base.ToString()}";

        private static string BuildMessage(string message, int statusCode, string response)
        {
            var truncated = response == null
                ? "(null)"
                : response.Substring(0, response.Length >= 512 ? 512 : response.Length);
            return $"{message}\n\nStatus: {statusCode}\nResponse: \n{truncated}";
        }
    }

    public partial class ApiException<TResult> : ApiException
    {
        public TResult Result { get; private set; }

        public ApiException(
            string message,
            int statusCode,
            string response,
            IReadOnlyDictionary<string, IEnumerable<string>> headers,
            TResult result,
            Exception innerException)
            : base(message, statusCode, response, headers, innerException)
        {
            Result = result;
        }
    }
}
