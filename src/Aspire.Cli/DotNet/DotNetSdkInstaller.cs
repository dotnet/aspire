// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.DotNet;

/// <summary>
/// Default implementation of <see cref="IDotNetSdkInstaller"/> that checks for dotnet on the system PATH.
/// </summary>
internal sealed class DotNetSdkInstaller : IDotNetSdkInstaller
{
    /// <inheritdoc />
    public async Task<bool> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);
            
            return process.ExitCode == 0;
        }
        catch
        {
            // If we can't start the process, the SDK is not available
            return false;
        }
    }

    /// <inheritdoc />
    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        // Reserved for future implementation
        throw new NotImplementedException("SDK installation is not yet implemented.");
    }
}