using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// Talks to the payment endpoints of the backend API.
/// </summary>
public sealed class PaymentApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public PaymentApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>POST payments/pay. Returns the id of the recorded payment.</summary>
    /// <exception cref="ApiException">The request failed; the message is user-friendly.</exception>
    public async Task<Guid> PayAsync(string rfid, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "payments/pay", new PayRequest(rfid, amount), JsonOptions, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new ApiException("This kiosk is not authorized to use the service. Please contact the staff.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException("We couldn't record your payment right now.");
            }

            // The response body is the payment id, either as plain text or as a JSON string.
            var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim().Trim('"');
            return Guid.TryParse(body, out var paymentId)
                ? paymentId
                : throw new ApiException("We received an unexpected response from the payment service.");
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Cannot reach the server.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports a timeout as a cancellation that we didn't ask for.
            throw new ApiException("The server took too long to respond.", ex);
        }
    }

    private sealed record PayRequest(string Rfid, decimal Amount);
}