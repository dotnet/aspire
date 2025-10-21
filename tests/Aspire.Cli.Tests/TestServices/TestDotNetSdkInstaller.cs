// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestDotNetSdkInstaller : IDotNetSdkInstaller
{
    public Func<CancellationToken, (bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)>? CheckAsyncCallback { get; set; }
    public Func<CancellationToken, Task>? InstallAsyncCallback { get; set; }

    public Task<(bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)> CheckAsync(CancellationToken cancellationToken = default)
    {
        return CheckAsyncCallback != null
            ? Task.FromResult(CheckAsyncCallback(cancellationToken))
            : Task.FromResult<(bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)>((true, "9.0.302", "9.0.302", false)); // Default to SDK available
    }

    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        return InstallAsyncCallback != null
            ? InstallAsyncCallback(cancellationToken)
            : throw new NotImplementedException();
    }
}