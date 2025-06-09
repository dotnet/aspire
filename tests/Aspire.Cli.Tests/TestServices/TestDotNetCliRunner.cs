// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestDotNetCliRunner : IDotNetCliRunner
{
    public Func<FileInfo, string, string, string?, DotNetCliRunnerInvocationOptions, CancellationToken, int>? AddPackageAsyncCallback { get; set; }
    public Func<FileInfo, DotNetCliRunnerInvocationOptions, CancellationToken, int>? BuildAsyncCallback { get; set; }
    public Func<DotNetCliRunnerInvocationOptions, CancellationToken, int>? CheckHttpCertificateAsyncCallback { get; set; }
    public Func<FileInfo, DotNetCliRunnerInvocationOptions, CancellationToken, (int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)>? GetAppHostInformationAsyncCallback { get; set; }
    public Func<FileInfo, string[], string[], DotNetCliRunnerInvocationOptions, CancellationToken, (int ExitCode, JsonDocument? Output)>? GetProjectItemsAndPropertiesAsyncCallback { get; set; }
    public Func<string, string, string?, bool, DotNetCliRunnerInvocationOptions, CancellationToken, (int ExitCode, string? TemplateVersion)>? InstallTemplateAsyncCallback { get; set; }
    public Func<string, string, string, DotNetCliRunnerInvocationOptions, CancellationToken, int>? NewProjectAsyncCallback { get; set; }
    public Func<FileInfo, bool, bool, string[], IDictionary<string, string>?, TaskCompletionSource<IAppHostBackchannel>?, DotNetCliRunnerInvocationOptions, CancellationToken, Task<int>>? RunAsyncCallback { get; set; }
    public Func<DirectoryInfo, string, bool, int, int, string?, DotNetCliRunnerInvocationOptions, CancellationToken, (int ExitCode, NuGetPackage[]? Packages)>? SearchPackagesAsyncCallback { get; set; }
    public Func<DotNetCliRunnerInvocationOptions, CancellationToken, int>? TrustHttpCertificateAsyncCallback { get; set; }

    public Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return AddPackageAsyncCallback != null
            ? Task.FromResult(AddPackageAsyncCallback(projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> BuildAsync(FileInfo projectFilePath, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return BuildAsyncCallback != null
            ? Task.FromResult(BuildAsyncCallback(projectFilePath, options, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> CheckHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return CheckHttpCertificateAsyncCallback != null
            ? Task.FromResult(CheckHttpCertificateAsyncCallback(options, cancellationToken))
            : Task.FromResult(0); // Return success if not overridden.
    }

    public Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        var informationalVersion = VersionHelper.GetDefaultTemplateVersion();

        return GetAppHostInformationAsyncCallback != null
            ? Task.FromResult(GetAppHostInformationAsyncCallback(projectFile, options, cancellationToken))
            : Task.FromResult<(int, bool, string?)>((0, true, informationalVersion));
    }

    public Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return GetProjectItemsAndPropertiesAsyncCallback != null
            ? Task.FromResult(GetProjectItemsAndPropertiesAsyncCallback(projectFile, items, properties, options, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return InstallTemplateAsyncCallback != null
            ? Task.FromResult(InstallTemplateAsyncCallback(packageName, version, nugetSource, force, options, cancellationToken))
            : Task.FromResult<(int, string?)>((0, version)); // If not overridden, just return success for the version specified.
    }

    public Task<int> NewProjectAsync(string templateName, string name, string outputPath, string[] extraArgs, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return NewProjectAsyncCallback != null
            ? Task.FromResult(NewProjectAsyncCallback(templateName, name, outputPath, options, cancellationToken))
            : Task.FromResult(0); // If not overridden, just return success.
    }

    public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return RunAsyncCallback != null
            ? RunAsyncCallback(projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken)
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return SearchPackagesAsyncCallback != null
            ? Task.FromResult(SearchPackagesAsyncCallback(workingDirectory, query, prerelease, take, skip, nugetSource, options, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> TrustHttpCertificateAsync(DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return TrustHttpCertificateAsyncCallback != null
            ? Task.FromResult(TrustHttpCertificateAsyncCallback(options, cancellationToken))
            : throw new NotImplementedException();
    }
}
