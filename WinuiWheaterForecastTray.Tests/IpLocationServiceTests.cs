using FluentAssertions;
using System.Net;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

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
        // Both primary (ipapi.co) and fallback (ip-api.com) go through the same mock, so both return an error.
        const string errorJson = """{"error":true,"reason":"RateLimited","latitude":0,"longitude":0}""";
        const string fallbackErrorJson = """{"status":"fail"}""";

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

    // C-01: when primary (ipapi.co) throws, fallback uses ip-api.com shape (status:"success", lat/lon fields)
    [Fact]
    public async Task GetLocationAsync_PrimaryThrows_FallbackReturnsCoordinates()
    {
        const string fallbackJson = """{"status":"success","lat":-12.2664,"lon":-38.9663,"city":"Feira de Santana"}""";
        // First call throws (simulates network error on primary); second call returns fallback JSON
        var handler = new MockHttpMessageHandler(fallbackJson, HttpStatusCode.OK, firstJson: null);
        var service = new IpLocationService(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });

        var result = await service.GetLocationAsync();

        result.Should().NotBeNull("ip-api.com fallback with status:success must return coordinates");
        result!.Value.Latitude.Should().BeApproximately(-12.2664, 0.001);
        result!.Value.Longitude.Should().BeApproximately(-38.9663, 0.001);
    }

    private static HttpClient MakeClient(string json)
        => new(new MockHttpMessageHandler(json)) { Timeout = TimeSpan.FromSeconds(5) };
}
