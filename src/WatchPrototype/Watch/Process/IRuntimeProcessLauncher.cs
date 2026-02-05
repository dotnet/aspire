// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Process launcher that triggers process launches at runtime of the watched application,
/// as opposed to design-time configuration given on command line or by the project system.
/// </summary>
internal interface IRuntimeProcessLauncher : IAsyncDisposable
{
    IEnumerable<(string name, string value)> GetEnvironmentVariables();

    /// <summary>
    /// Initiates shutdown. Terminates all created processes.
    /// </summary>
    ValueTask TerminateLaunchedProcessesAsync(CancellationToken cancellationToken);
}
