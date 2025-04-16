// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestDotNetCliRunner : IDotNetCliRunner
{
    public Func<FileInfo, string, string, CancellationToken, int>? AddPackageAsyncCallback { get; set; }
    public Func<FileInfo, CancellationToken, int>? BuildAsyncCallback { get; set; }
    public Func<CancellationToken, int>? CheckHttpCertificateAsyncCallback { get; set; }
    public Func<FileInfo, CancellationToken, (int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)>? GetAppHostInformationAsyncCallback { get; set; }
    public Func<FileInfo, string[], string[], CancellationToken, (int ExitCode, JsonDocument? Output)>? GetProjectItemsAndPropertiesAsyncCallback { get; set; }
    public Func<string, string, string?, bool, CancellationToken, (int ExitCode, string? TemplateVersion)>? InstallTemplateAsyncCallback { get; set; }
    public Func<string, string, string, CancellationToken, int>? NewProjectAsyncCallback { get; set; }
    public Func<FileInfo, bool, bool, string[], IDictionary<string, string>?, TaskCompletionSource<AppHostBackchannel>?, CancellationToken, int>? RunAsyncCallback { get; set; }
    public Func<DirectoryInfo, string, bool, int, int, string?, CancellationToken, (int ExitCode, NuGetPackage[]? Packages)>? SearchPackagesAsyncCallback { get; set; }
    public Func<CancellationToken, int>? TrustHttpCertificateAsyncCallback { get; set; }

    public Task<int> AddPackageAsync(FileInfo projectFilePath, string packageName, string packageVersion, CancellationToken cancellationToken)
    {
        return AddPackageAsyncCallback != null
            ? Task.FromResult(AddPackageAsyncCallback(projectFilePath, packageName, packageVersion, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> BuildAsync(FileInfo projectFilePath, CancellationToken cancellationToken)
    {
        return BuildAsyncCallback != null
            ? Task.FromResult(BuildAsyncCallback(projectFilePath, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> CheckHttpCertificateAsync(CancellationToken cancellationToken)
    {
        return CheckHttpCertificateAsyncCallback != null
            ? Task.FromResult(CheckHttpCertificateAsyncCallback(cancellationToken))
            : Task.FromResult(0); // Return success if not overridden.
    }

    public Task<(int ExitCode, bool IsAspireHost, string? AspireHostingSdkVersion)> GetAppHostInformationAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var informationalVersion = VersionHelper.GetDefaultTemplateVersion();

        return GetAppHostInformationAsyncCallback != null
            ? Task.FromResult(GetAppHostInformationAsyncCallback(projectFile, cancellationToken))
            : Task.FromResult<(int, bool, string?)>((0, true, informationalVersion));
    }

    public Task<(int ExitCode, JsonDocument? Output)> GetProjectItemsAndPropertiesAsync(FileInfo projectFile, string[] items, string[] properties, CancellationToken cancellationToken)
    {
        return GetProjectItemsAndPropertiesAsyncCallback != null
            ? Task.FromResult(GetProjectItemsAndPropertiesAsyncCallback(projectFile, items, properties, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, string? TemplateVersion)> InstallTemplateAsync(string packageName, string version, string? nugetSource, bool force, CancellationToken cancellationToken)
    {
        return InstallTemplateAsyncCallback != null
            ? Task.FromResult(InstallTemplateAsyncCallback(packageName, version, nugetSource, force, cancellationToken))
            : Task.FromResult<(int, string?)>((0, version)); // If not overridden, just return success for the version specified.
    }

    public Task<int> NewProjectAsync(string templateName, string name, string outputPath, CancellationToken cancellationToken)
    {
        return NewProjectAsyncCallback != null
            ? Task.FromResult(NewProjectAsyncCallback(templateName, name, outputPath, cancellationToken))
            : Task.FromResult(0); // If not overridden, just return success.
    }

    public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<AppHostBackchannel>? backchannelCompletionSource, CancellationToken cancellationToken)
    {
        return RunAsyncCallback != null
            ? Task.FromResult(RunAsyncCallback(projectFile, watch, noBuild, args, env, backchannelCompletionSource, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<(int ExitCode, NuGetPackage[]? Packages)> SearchPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, int take, int skip, string? nugetSource, CancellationToken cancellationToken)
    {
        return SearchPackagesAsyncCallback != null
            ? Task.FromResult(SearchPackagesAsyncCallback(workingDirectory, query, prerelease, take, skip, nugetSource, cancellationToken))
            : throw new NotImplementedException();
    }

    public Task<int> TrustHttpCertificateAsync(CancellationToken cancellationToken)
    {
        return TrustHttpCertificateAsyncCallback != null
            ? Task.FromResult(TrustHttpCertificateAsyncCallback(cancellationToken))
            : throw new NotImplementedException();
    }
}
