// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Provides the ability to send input to a resource's stdin stream.
/// </summary>
internal interface IResourceConsoleInputService
{
    /// <summary>
    /// Sends input to the specified resource's stdin stream.
    /// </summary>
    /// <param name="resourceName">The name of the resource to send input to.</param>
    /// <param name="input">The input data to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the input has been sent.</returns>
    Task SendInputAsync(string resourceName, string input, CancellationToken cancellationToken);
}
