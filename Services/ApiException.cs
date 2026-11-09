using System;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// A failed API call. The message is safe to show to the user.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}