using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElectronicPaymentApp.Models;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// Talks to the statement-of-accounts endpoints of the backend API.
/// </summary>
public sealed class AccountApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public AccountApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>GET /statement-of-accounts/check-info?rfid=...</summary>
    /// <exception cref="ApiException">The request failed; the message is user-friendly.</exception>
    public async Task<StudentAccount> GetAccountAsync(string rfid, CancellationToken cancellationToken = default)
    {
        var url = $"statement-of-accounts/check-info?rfid={Uri.EscapeDataString(rfid)}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ApiException("This ID is not registered. Please try another ID.");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new ApiException("This kiosk is not authorized to use the service. Please contact the staff.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException("We couldn't check your account right now. Please try again.");
            }

            var account = await response.Content.ReadFromJsonAsync<StudentAccount>(JsonOptions, cancellationToken);
            return account ?? throw new ApiException("We received an unexpected response. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Cannot reach the server. Please try again.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports a timeout as a cancellation that we didn't ask for.
            throw new ApiException("The server took too long to respond. Please try again.", ex);
        }
        catch (JsonException ex)
        {
            throw new ApiException("We received an unexpected response. Please try again.", ex);
        }
    }
}