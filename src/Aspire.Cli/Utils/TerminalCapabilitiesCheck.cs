// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Checks terminal and environment capabilities (ANSI support, interactivity).
/// </summary>
internal sealed class TerminalCapabilitiesCheck(ICliHostEnvironment hostEnvironment) : IEnvironmentCheck
{
    public int Order => 10; // Fast check - environment variables

    public Task<EnvironmentCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new List<string>();

        if (hostEnvironment.SupportsAnsi)
        {
            capabilities.Add("colors");
        }

        if (hostEnvironment.SupportsInteractiveInput)
        {
            capabilities.Add("interactive input");
        }

        if (hostEnvironment.SupportsInteractiveOutput)
        {
            capabilities.Add("interactive output");
        }

        if (capabilities.Count == 0)
        {
            return Task.FromResult(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "terminal",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Terminal has limited capabilities"
            });
        }

        return Task.FromResult(new EnvironmentCheckResult
        {
            Category = "environment",
            Name = "terminal",
            Status = EnvironmentCheckStatus.Pass,
            Message = $"Terminal supports: {string.Join(", ", capabilities)}"
        });
    }
}
