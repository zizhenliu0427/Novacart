using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class CurrencyServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private sealed class MemoryCache : IRedisCacheService
    {
        private readonly Dictionary<string, string> _store = new();
        public Task<T?> GetAsync<T>(string key) where T : class
        {
            if (!_store.TryGetValue(key, out var json)) return Task.FromResult<T?>(null);
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json));
        }
        public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            _store[key] = System.Text.Json.JsonSerializer.Serialize(value);
            return Task.CompletedTask;
        }
        public Task RemoveAsync(string key) { _store.Remove(key); return Task.CompletedTask; }
        public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
    }

    [Fact]
    public async Task GetRatesAsync_ParsesFrankfurterResponse_AndCaches()
    {
        var json = """
            {"amount":1.0,"base":"AUD","date":"2026-07-14","rates":{"USD":0.69,"CNY":4.71,"JPY":112.6,"SGD":0.90,"GBP":0.51,"NZD":1.09}}
            """;
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        var http = new HttpClient(handler);
        var cache = new MemoryCache();
        var svc = new CurrencyService(http, cache, NullLogger<CurrencyService>.Instance);

        var first = await svc.GetRatesAsync();
        first.Base.Should().Be("AUD");
        first.Rates["USD"].Should().Be(0.69m);
        first.Source.Should().Be("live");

        // Second call should hit cache (handler would throw if called again).
        var handler2 = new StubHandler(_ => throw new InvalidOperationException("Should use cache"));
        var svc2 = new CurrencyService(new HttpClient(handler2), cache, NullLogger<CurrencyService>.Instance);
        var second = await svc2.GetRatesAsync();
        second.Source.Should().Be("cache");
        second.Rates["CNY"].Should().Be(4.71m);
    }

    [Fact]
    public async Task GetRatesAsync_FallsBackWhenProviderFails()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler);
        var cache = new MemoryCache();
        var svc = new CurrencyService(http, cache, NullLogger<CurrencyService>.Instance);

        var rates = await svc.GetRatesAsync();
        rates.Source.Should().Be("fallback");
        rates.Rates.Should().ContainKey("USD");
        rates.Rates.Should().ContainKey("CNY");
        rates.Rates.Should().ContainKey("GBP");
        rates.Rates.Should().ContainKey("NZD");
    }

    [Fact]
    public async Task GetRatesAsync_ReturnsStaleCacheWhenProviderFails()
    {
        var liveJson = """
            {"amount":1.0,"base":"AUD","date":"2026-07-14","rates":{"USD":0.69,"CNY":4.71,"JPY":112.6,"SGD":0.90,"GBP":0.51,"NZD":1.09}}
            """;
        var liveHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(liveJson, Encoding.UTF8, "application/json"),
        });
        var cache = new MemoryCache();
        var liveSvc = new CurrencyService(new HttpClient(liveHandler), cache, NullLogger<CurrencyService>.Instance);
        await liveSvc.GetRatesAsync();

        var failHandler = new StubHandler(_ => throw new HttpRequestException("Provider down"));
        var failSvc = new CurrencyService(new HttpClient(failHandler), cache, NullLogger<CurrencyService>.Instance);

        var stale = await failSvc.GetRatesAsync();

        stale.Source.Should().Be("cache-stale");
        stale.Rates["GBP"].Should().Be(0.51m);
        stale.Rates["NZD"].Should().Be(1.09m);
    }

    [Fact]
    public async Task GetRatesAsync_RequestsAllSupportedSymbols()
    {
        string? requestedUrl = null;
        var handler = new StubHandler(req =>
        {
            requestedUrl = req.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"amount":1.0,"base":"AUD","date":"2026-07-14","rates":{"USD":0.69,"CNY":4.71,"JPY":112.6,"SGD":0.90,"GBP":0.51,"NZD":1.09}}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        var svc = new CurrencyService(new HttpClient(handler), new MemoryCache(), NullLogger<CurrencyService>.Instance);

        await svc.GetRatesAsync();

        requestedUrl.Should().Contain("base=AUD");
        requestedUrl.Should().Contain("USD");
        requestedUrl.Should().Contain("GBP");
        requestedUrl.Should().Contain("NZD");
    }
}
