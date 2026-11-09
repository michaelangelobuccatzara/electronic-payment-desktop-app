using System;
using System.IO;
using System.Text.Json;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// API configuration read from appsettings.json next to the executable.
/// </summary>
public sealed class ApiSettings
{
    /// <summary>Name of the HTTP header that carries the API key. Change it if the API expects another name.</summary>
    public const string ApiKeyHeaderName = "X-API-Key";

    private ApiSettings(Uri baseAddress, string apiKey)
    {
        BaseAddress = baseAddress;
        ApiKey = apiKey;
    }

    public Uri BaseAddress { get; }

    public string ApiKey { get; }

    public static ApiSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Configuration file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("Api", out var api))
        {
            throw new InvalidOperationException("appsettings.json must contain an \"Api\" section.");
        }

        var baseUrl = ReadRequiredString(api, "BaseUrl");
        var apiKey = ReadRequiredString(api, "ApiKey");

        // A trailing slash makes relative request paths resolve under the base address.
        return new ApiSettings(new Uri(baseUrl.TrimEnd('/') + "/"), apiKey);
    }

    private static string ReadRequiredString(JsonElement section, string name)
    {
        if (section.TryGetProperty(name, out var element) &&
            element.GetString() is { Length: > 0 } value &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"appsettings.json must contain Api:{name}.");
    }
}