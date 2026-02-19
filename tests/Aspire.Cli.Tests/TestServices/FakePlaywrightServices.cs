// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.Playwright;
using Aspire.Cli.Npm;
using Semver;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// A fake implementation of <see cref="INpmRunner"/> for testing.
/// </summary>
internal sealed class FakeNpmRunner : INpmRunner
{
    public Task<NpmPackageInfo?> ResolvePackageAsync(string packageName, string versionRange, CancellationToken cancellationToken)
        => Task.FromResult<NpmPackageInfo?>(null);

    public Task<string?> PackAsync(string packageName, string version, string outputDirectory, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    public Task<bool> AuditSignaturesAsync(CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> InstallGlobalAsync(string tarballPath, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

/// <summary>
/// A fake implementation of <see cref="IPlaywrightCliRunner"/> for testing.
/// </summary>
internal sealed class FakePlaywrightCliRunner : IPlaywrightCliRunner
{
    public Task<SemVersion?> GetVersionAsync(CancellationToken cancellationToken)
        => Task.FromResult<SemVersion?>(null);

    public Task<bool> InstallSkillsAsync(CancellationToken cancellationToken)
        => Task.FromResult(true);
}
