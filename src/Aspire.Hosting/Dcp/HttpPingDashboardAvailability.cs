// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dcp;

internal sealed class HttpPingDashboardAvailability : IDashboardAvailability
{
    private readonly ILogger<HttpPingDashboardAvailability> _logger;

    public HttpPingDashboardAvailability(ILogger<HttpPingDashboardAvailability> logger)
    {
        _logger = logger;
    }

    public async Task WaitForDashboardAvailabilityAsync(Uri url, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var client = new HttpClient();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Dashboard not ready yet.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), linkedCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Only display this error if the timeout CTS was the one that was cancelled.
            throw new DistributedApplicationException($"Timed out after {timeout} while waiting for the dashboard to be responsive.");
        }
    }
}
