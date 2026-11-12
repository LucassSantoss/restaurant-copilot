using System.Net;
using System.Text;

namespace RestaurantCopilot.Infrastructure.Tests.Ai;

/// <summary>Returns queued responses in order and records every request (with its body) it receives.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    public FakeHttpMessageHandler RespondWith(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));

        return _responses.Count > 0
            ? _responses.Dequeue()()
            : throw new InvalidOperationException("No fake response queued for this request.");
    }
}
