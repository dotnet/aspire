// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestDotNetSdkInstaller : IDotNetSdkInstaller
{
    public Func<CancellationToken, bool>? CheckAsyncCallback { get; set; }
    public Func<string, CancellationToken, bool>? CheckAsyncWithVersionCallback { get; set; }
    public Func<CancellationToken, Task>? InstallAsyncCallback { get; set; }
    public Func<CancellationToken, string?>? GetInstalledSdkVersionAsyncCallback { get; set; }
    public Func<string>? GetEffectiveMinimumSdkVersionCallback { get; set; }

    public Task<bool> CheckAsync(CancellationToken cancellationToken = default)
    {
        return CheckAsyncCallback != null
            ? Task.FromResult(CheckAsyncCallback(cancellationToken))
            : Task.FromResult(true); // Default to SDK available
    }

    public Task<bool> CheckAsync(string minimumVersion, CancellationToken cancellationToken = default)
    {
        return CheckAsyncWithVersionCallback != null
            ? Task.FromResult(CheckAsyncWithVersionCallback(minimumVersion, cancellationToken))
            : Task.FromResult(true); // Default to SDK available
    }

    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        return InstallAsyncCallback != null
            ? InstallAsyncCallback(cancellationToken)
            : throw new NotImplementedException();
    }

    public Task<string?> GetInstalledSdkVersionAsync(CancellationToken cancellationToken = default)
    {
        return GetInstalledSdkVersionAsyncCallback != null
            ? Task.FromResult(GetInstalledSdkVersionAsyncCallback(cancellationToken))
            : Task.FromResult<string?>("9.0.302"); // Default version
    }

    public string GetEffectiveMinimumSdkVersion()
    {
        return GetEffectiveMinimumSdkVersionCallback?.Invoke() ?? "9.0.302"; // Default version
    }
}