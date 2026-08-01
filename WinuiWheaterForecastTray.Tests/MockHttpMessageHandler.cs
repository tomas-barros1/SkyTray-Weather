using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Tests;

/// <summary>Simple test double that returns a pre-configured <see cref="HttpResponseMessage"/>.</summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    /// <summary>Convenience constructor: returns <paramref name="json"/> with <paramref name="statusCode"/> for every request.</summary>
    public MockHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        : this(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })
    { }

    /// <summary>
    /// Convenience constructor: first request returns <paramref name="firstStatusCode"/> with
    /// <paramref name="firstJson"/>; all subsequent requests return <paramref name="json"/> with 200.
    /// Pass <c>firstJson = null</c> to simulate a network error on the first call.
    /// </summary>
    public MockHttpMessageHandler(string json, HttpStatusCode firstStatusCode, string? firstJson)
    {
        int calls = 0;
        _handler = _ =>
        {
            if (calls++ == 0)
            {
                if (firstJson is null)
                    throw new HttpRequestException("Simulated network failure on first call.");

                return new HttpResponseMessage(firstStatusCode)
                {
                    Content = new StringContent(firstJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
