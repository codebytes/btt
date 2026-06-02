using System.Globalization;
using System.Text.Json;
using BarTabTracker.Server.Api;
using StackExchange.Redis;

namespace BarTabTracker.Server.Location;

public interface IReverseGeocoder
{
    Task<ReverseGeocodeResponse> ReverseGeocode(double lat, double lng, CancellationToken cancellationToken);
}

public sealed class NominatimReverseGeocoder(HttpClient httpClient, IConnectionMultiplexer connection) : IReverseGeocoder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(30);
    private readonly IDatabase database = connection.GetDatabase();

    public async Task<ReverseGeocodeResponse> ReverseGeocode(double lat, double lng, CancellationToken cancellationToken)
    {
        var key = CacheKey(lat, lng);
        var cached = await database.StringGetAsync(key);
        if (cached.HasValue)
        {
            var cachedResponse = JsonSerializer.Deserialize<ReverseGeocodeResponse>((string)cached!, JsonOptions);
            if (cachedResponse is not null)
            {
                return cachedResponse;
            }
        }

        try
        {
            var url = string.Create(CultureInfo.InvariantCulture, $"reverse?format=json&lat={lat}&lon={lng}&zoom=18&addressdetails=1&extratags=1");
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await CacheFallback(key);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var address = root.TryGetProperty("address", out var addressElement) ? addressElement : default;
            var country = GetString(address, "country");
            var countryCode = GetString(address, "country_code")?.ToUpperInvariant();
            var barName = GetString(root, "name")
                ?? GetString(address, "pub")
                ?? GetString(address, "bar")
                ?? GetString(address, "restaurant");
            var currency = CurrencyLookup.ForCountryCode(countryCode);
            var result = new ReverseGeocodeResponse(barName, country, currency, IsFallback: false);

            await database.StringSetAsync(key, JsonSerializer.Serialize(result, JsonOptions), CacheDuration);
            return result;
        }
        catch (HttpRequestException)
        {
            return await CacheFallback(key);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await CacheFallback(key);
        }
        catch (JsonException)
        {
            return await CacheFallback(key);
        }
    }

    private async Task<ReverseGeocodeResponse> CacheFallback(string key)
    {
        var result = new ReverseGeocodeResponse(null, null, "USD", IsFallback: true);
        await database.StringSetAsync(key, JsonSerializer.Serialize(result, JsonOptions), TimeSpan.FromHours(6));
        return result;
    }

    private static string CacheKey(double lat, double lng) => string.Create(
        CultureInfo.InvariantCulture,
        $"geocode:reverse:{Math.Round(lat, 3, MidpointRounding.AwayFromZero):F3}:{Math.Round(lng, 3, MidpointRounding.AwayFromZero):F3}");

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}

internal static class CurrencyLookup
{
    private static readonly IReadOnlyDictionary<string, string> Currencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "USD",
        ["CA"] = "CAD",
        ["GB"] = "GBP",
        ["IE"] = "EUR",
        ["FR"] = "EUR",
        ["DE"] = "EUR",
        ["ES"] = "EUR",
        ["IT"] = "EUR",
        ["NL"] = "EUR",
        ["BE"] = "EUR",
        ["PT"] = "EUR",
        ["AT"] = "EUR",
        ["FI"] = "EUR",
        ["GR"] = "EUR",
        ["LU"] = "EUR",
        ["MT"] = "EUR",
        ["CY"] = "EUR",
        ["EE"] = "EUR",
        ["LV"] = "EUR",
        ["LT"] = "EUR",
        ["SK"] = "EUR",
        ["SI"] = "EUR",
        ["AU"] = "AUD",
        ["NZ"] = "NZD",
        ["JP"] = "JPY",
        ["CN"] = "CNY",
        ["IN"] = "INR",
        ["BR"] = "BRL",
        ["MX"] = "MXN",
        ["CH"] = "CHF",
        ["SE"] = "SEK",
        ["NO"] = "NOK",
        ["DK"] = "DKK",
        ["PL"] = "PLN",
        ["CZ"] = "CZK",
        ["ZA"] = "ZAR"
    };

    public static string ForCountryCode(string? countryCode) => countryCode is not null && Currencies.TryGetValue(countryCode, out var currency)
        ? currency
        : "USD";
}
