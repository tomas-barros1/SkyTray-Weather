using FluentAssertions;
using System.Net;
using WinuiWeatherForecastTray.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public class IpLocationServiceTests
{
    // C-02: valid response without error flag is accepted regardless of coordinate values
    [Fact]
    public async Task GetLocationAsync_ValidIpapiResponse_ReturnsCoordinates()
    {
        const string json = """{"latitude":-12.2337,"longitude":-39.0656,"city":"Feira de Santana","error":false}""";
        var service = new IpLocationService(MakeClient(json));

        var result = await service.GetLocationAsync();

        result.Should().NotBeNull("valid response with error:false must be accepted");
        result!.Value.Latitude.Should().BeApproximately(-12.2337, 0.001);
        result!.Value.Longitude.Should().BeApproximately(-39.0656, 0.001);
    }

    // C-02: (0,0) with no error flag must be accepted — Gulf of Guinea is a real location
    [Fact]
    public async Task GetLocationAsync_ZeroZeroCoordinateNoError_IsAccepted()
    {
        const string json = """{"latitude":0.0,"longitude":0.0,"city":"NullIsland","error":false}""";
        var service = new IpLocationService(MakeClient(json));

        var result = await service.GetLocationAsync();

        result.Should().NotBeNull("(0,0) is a valid geographic location and must not be silently rejected");
        result!.Value.Latitude.Should().Be(0.0);
        result!.Value.Longitude.Should().Be(0.0);
    }

    // C-02: error:true response is rejected — do not use coordinates from error body
    [Fact]
    public async Task GetLocationAsync_IpapiErrorFlag_ReturnsNull()
    {
        // Both primary (ipapi.co) and fallback (ipwho.is) go through the same mock, so both return an error.
        const string errorJson = """{"error":true,"reason":"RateLimited","latitude":0,"longitude":0}""";
        const string fallbackErrorJson = """{"success":false,"message":"Rate limit reached"}""";

        int call = 0;
        var handler = new MockHttpMessageHandler(req =>
        {
            return call++ == 0
                ? new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
                { Content = new System.Net.Http.StringContent(errorJson, System.Text.Encoding.UTF8, "application/json") }
                : new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
                { Content = new System.Net.Http.StringContent(fallbackErrorJson, System.Text.Encoding.UTF8, "application/json") };
        });
        var service = new IpLocationService(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });

        var result = await service.GetLocationAsync();

        result.Should().BeNull("when both primary and fallback fail no location should be returned");
    }

    // C-01: when primary (ipapi.co) throws, fallback uses the ipwho.is shape (success:true, latitude/longitude)
    [Fact]
    public async Task GetLocationAsync_PrimaryThrows_FallbackReturnsCoordinates()
    {
        const string fallbackJson = """{"success":true,"latitude":-12.2664,"longitude":-38.9663,"city":"Feira de Santana"}""";
        // First call throws (simulates network error on primary); second call returns fallback JSON
        var handler = new MockHttpMessageHandler(fallbackJson, HttpStatusCode.OK, firstJson: null);
        var service = new IpLocationService(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });

        var result = await service.GetLocationAsync();

        result.Should().NotBeNull("ipwho.is fallback with success:true must return coordinates");
        result!.Value.Latitude.Should().BeApproximately(-12.2664, 0.001);
        result!.Value.Longitude.Should().BeApproximately(-38.9663, 0.001);
    }

    // Coordinates come from the network: out-of-range or missing values must never be returned (or cached).
    [Theory]
    [InlineData("""{"latitude":123.4,"longitude":10.0,"error":false}""")]   // latitude out of range
    [InlineData("""{"latitude":10.0,"longitude":-200.0,"error":false}""")]  // longitude out of range
    [InlineData("""{"city":"NoCoordinates","error":false}""")]              // fields missing: must not become (0,0)
    public async Task GetLocationAsync_InvalidPrimaryCoordinates_AreRejected(string primaryJson)
    {
        const string fallbackErrorJson = """{"success":false,"message":"nope"}""";
        var service = new IpLocationService(MakeSequenceClient(primaryJson, fallbackErrorJson));

        var result = await service.GetLocationAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationAsync_InvalidPrimaryCoordinates_FallsThroughToValidFallback()
    {
        const string badPrimary = """{"latitude":999,"longitude":10.0,"error":false}""";
        const string goodFallback = """{"success":true,"latitude":-12.2664,"longitude":-38.9663}""";
        var service = new IpLocationService(MakeSequenceClient(badPrimary, goodFallback));

        var result = await service.GetLocationAsync();

        result.Should().NotBeNull();
        result!.Value.Latitude.Should().BeApproximately(-12.2664, 0.001);
    }

    [Theory]
    [InlineData("""{"success":true,"latitude":95.0,"longitude":10.0}""")]  // out of range
    [InlineData("""{"success":true}""")]                                     // missing coordinates
    [InlineData("""{"success":false,"latitude":10.0,"longitude":10.0}""")]  // not successful
    public async Task GetLocationAsync_InvalidFallbackResponse_ReturnsNull(string fallbackJson)
    {
        const string primaryError = """{"error":true,"reason":"RateLimited"}""";
        var service = new IpLocationService(MakeSequenceClient(primaryError, fallbackJson));

        var result = await service.GetLocationAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationAsync_FallbackIsRequestedOverHttps()
    {
        var requestedUrls = new List<string>();
        var handler = new MockHttpMessageHandler(req =>
        {
            requestedUrls.Add(req.RequestUri!.ToString());
            return new System.Net.Http.HttpResponseMessage(HttpStatusCode.InternalServerError);
        });
        var service = new IpLocationService(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });

        await service.GetLocationAsync();

        requestedUrls.Should().HaveCount(2);
        requestedUrls.Should().OnlyContain(u => u.StartsWith("https://"), "location lookups must not use cleartext HTTP");
    }

    /// <summary>Returns <paramref name="first"/> for the first request and <paramref name="second"/> for the next.</summary>
    private static HttpClient MakeSequenceClient(string first, string second)
    {
        int call = 0;
        var handler = new MockHttpMessageHandler(_ => new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(call++ == 0 ? first : second, System.Text.Encoding.UTF8, "application/json")
        });
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
    }

    private static HttpClient MakeClient(string json)
        => new(new MockHttpMessageHandler(json)) { Timeout = TimeSpan.FromSeconds(5) };
}
