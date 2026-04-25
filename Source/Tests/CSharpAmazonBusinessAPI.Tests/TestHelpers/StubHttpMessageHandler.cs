using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace CSharpAmazonBusinessAPI.Tests.TestHelpers;

// Records every request the pipeline sends, returns canned responses in order.
// If more requests come in than canned responses, throws — caught by the test.
public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _handlers = new();
    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public void Enqueue(HttpStatusCode status, string body = "", Action<HttpResponseMessage>? customize = null)
    {
        _handlers.Enqueue(req =>
        {
            var resp = new HttpResponseMessage(status)
            {
                Content = new StringContent(body),
                RequestMessage = req,
            };
            customize?.Invoke(resp);
            return resp;
        });
    }

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handlers.Enqueue(handler);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(CloneForCapture(request));
        if (_handlers.Count == 0)
            throw new InvalidOperationException($"No handler queued for {request.Method} {request.RequestUri}");
        return Task.FromResult(_handlers.Dequeue()(request));
    }

    // Headers and method/uri are what tests inspect — body cloning omitted (LWA POSTs read content
    // before our test would get to it).
    private static HttpRequestMessage CloneForCapture(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        foreach (var h in req.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        return clone;
    }
}
