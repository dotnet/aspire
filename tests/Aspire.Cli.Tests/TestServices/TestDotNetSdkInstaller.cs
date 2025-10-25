// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestDotNetSdkInstaller : IDotNetSdkInstaller
{
    public Func<CancellationToken, CheckInstallResult>? CheckAsyncCallback { get; set; }
    public Func<CancellationToken, Task>? InstallAsyncCallback { get; set; }

    public Task<CheckInstallResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return CheckAsyncCallback != null
            ? Task.FromResult(CheckAsyncCallback(cancellationToken))
            : Task.FromResult(new CheckInstallResult
            {
                Success = true,
                HighestVersion = "9.0.302",
                MinimumRequiredVersion = "9.0.302",
                ForceInstall = false,
                ShouldInstall = false
            }); // Default to SDK available
    }

    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        return InstallAsyncCallback != null
            ? InstallAsyncCallback(cancellationToken)
            : throw new NotImplementedException();
    }

    public string GetEffectiveMinimumSdkVersion()
    {
        return "9.0.302"; // Default test value
    }
}