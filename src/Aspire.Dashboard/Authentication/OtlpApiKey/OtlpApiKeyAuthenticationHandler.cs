// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication.OtlpApiKey;

public class OtlpApiKeyAuthenticationHandler : AuthenticationHandler<OtlpApiKeyAuthenticationHandlerOptions>
{
    public const string ApiKeyHeaderName = "x-otlp-api-key";

    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;

    public OtlpApiKeyAuthenticationHandler(IOptionsMonitor<DashboardOptions> dashboardOptions, IOptionsMonitor<OtlpApiKeyAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
        _dashboardOptions = dashboardOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = _dashboardOptions.CurrentValue.Otlp;

        if (Context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            // There must be only one header with the API key.
            if (apiKey.Count != 1)
            {
                return Task.FromResult(AuthenticateResult.Fail($"Multiple '{ApiKeyHeaderName}' headers in request."));
            }

            if (!CompareApiKey(options.GetPrimaryApiKeyBytes(), apiKey.ToString()))
            {
                if (options.GetSecondaryApiKeyBytes() is not { } secondaryBytes || !CompareApiKey(secondaryBytes, apiKey.ToString()))
                {
                    return Task.FromResult(AuthenticateResult.Fail($"Incoming API key from '{ApiKeyHeaderName}' header doesn't match configured API key."));
                }
            }
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail($"API key from '{ApiKeyHeaderName}' header is missing."));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    // This method is used to compare two API keys in a way that avoids timing attacks.
    private static bool CompareApiKey(byte[] expectedApiKeyBytes, string requestApiKey)
    {
        const int StackAllocThreshold = 256;

        var requestByteCount = Encoding.UTF8.GetByteCount(requestApiKey);

        // API key will never match if lengths are different. But still do all the work to avoid timing attacks.
        var lengthsEqual = expectedApiKeyBytes.Length == requestByteCount;

        var requestSpanLength = Math.Max(requestByteCount, expectedApiKeyBytes.Length);
        byte[]? requestPooled = null;
        var requestBytesSpan = (requestSpanLength <= StackAllocThreshold ?
            stackalloc byte[StackAllocThreshold] :
            (requestPooled = RentClearedArray(requestSpanLength))).Slice(0, requestSpanLength);

        try
        {
            // Always succeeds because the byte span is always as big or bigger than required.
            Encoding.UTF8.GetBytes(requestApiKey, requestBytesSpan);

            // Trim request bytes to the same length as expected bytes. Need to be the same size for fixed time comparison.
            var equals = CryptographicOperations.FixedTimeEquals(expectedApiKeyBytes, requestBytesSpan.Slice(0, expectedApiKeyBytes.Length));

            return equals && lengthsEqual;
        }
        finally
        {
            if (requestPooled != null)
            {
                ArrayPool<byte>.Shared.Return(requestPooled);
            }
        }

        static byte[] RentClearedArray(int byteCount)
        {
            // UTF8 bytes are copied into the array but remaining bytes are untouched.
            // Because all bytes in the array are compared, clear the array to avoid comparing previous data.
            var array = ArrayPool<byte>.Shared.Rent(byteCount);
            Array.Clear(array);
            return array;
        }
    }
}

public static class OtlpApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "OtlpApiKey";
}

public sealed class OtlpApiKeyAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
