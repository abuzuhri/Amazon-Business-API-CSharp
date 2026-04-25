using System;
using System.Net;

namespace CSharpAmazonBusinessAPI.Exceptions
{
    public class AmazonBusinessException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseBody { get; }

        public AmazonBusinessException(string message) : base(message) { }

        public AmazonBusinessException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public AmazonBusinessException(string message, Exception inner) : base(message, inner) { }
    }

    public class AmazonBusinessUnauthorizedException : AmazonBusinessException
    {
        public AmazonBusinessUnauthorizedException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody) { }
    }

    public class AmazonBusinessInvalidInputException : AmazonBusinessException
    {
        public AmazonBusinessInvalidInputException(string message) : base(message) { }

        public AmazonBusinessInvalidInputException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody) { }
    }

    public class AmazonBusinessNotFoundException : AmazonBusinessException
    {
        public AmazonBusinessNotFoundException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody) { }
    }

    public class AmazonBusinessQuotaExceededException : AmazonBusinessException
    {
        public AmazonBusinessQuotaExceededException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody) { }
    }

    public class AmazonBusinessInternalErrorException : AmazonBusinessException
    {
        public AmazonBusinessInternalErrorException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody) { }
    }
}
