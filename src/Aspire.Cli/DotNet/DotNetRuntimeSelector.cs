// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.DotNet;

/// <summary>
/// Default implementation of <see cref="IDotNetRuntimeSelector"/> that uses the system .NET SDK.
/// </summary>
internal sealed class DotNetRuntimeSelector(
    ILogger<DotNetRuntimeSelector> logger,
    IDotNetSdkInstaller sdkInstaller) : IDotNetRuntimeSelector
{
    /// <inheritdoc />
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Initializing .NET runtime selector");
        
        // For now, just delegate to the existing SDK installer check
        // In the future, this could implement private SDK auto-installation
        var isSdkAvailable = await sdkInstaller.CheckAsync(cancellationToken);
        
        if (!isSdkAvailable)
        {
            logger.LogDebug("System .NET SDK is not available");
            // TODO: In the future, this could attempt to install a private SDK
            return false;
        }
        
        logger.LogDebug("System .NET SDK is available");
        return true;
    }
}