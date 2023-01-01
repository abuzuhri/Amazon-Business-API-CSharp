using System;

namespace CSharpAmazonBusinessAPI.Exceptions
{
    public class ApiException<T> : Exception
    {
        public ApiException() { }
        public ApiException(string message, int status_, string text, System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>> headers_, object p, object p1) { }

        //public ApiException(string message, Exception inner)
        //: base(message, inner) { }
    }
}
