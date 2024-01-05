// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Dashboard.Model;

internal static class IDashboardClientExtensions
{
    /// <summary>
    /// Gets the application name if available. If it's not yet available, an empty string is returned
    /// synchronously, and <paramref name="refresh"/> is invoked later on, when the value is available.
    /// The expectation is that callers pass <c>() => InvokeAsync(StateHasChanged)</c>.
    /// </summary>
    public static string FormatApplicationName(this IDashboardClient client, string pattern, Action refresh)
    {
        var task = client.WhenConnected;

        if (!task.IsCompleted)
        {
            // We're not yet connected. Schedule refresh to be invoked
            // when the connection is established.
            task.ContinueWith(
                _ => refresh(),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Current);

            // Return an empty string for now.
            return "";
        }

        // We are connected, so should have an application name.
        return string.Format(CultureInfo.InvariantCulture, pattern, client.ApplicationName);
    }
}
