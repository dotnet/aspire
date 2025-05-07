// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Builds;

internal interface IAppHostBuilder
{
    Task<int> BuildAppHostAsync(FileInfo projectFile, bool useCache, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken);
}

internal sealed class AppHostBuilder(ILogger<AppHostBuilder> logger, IDotNetCliRunner runner) : IAppHostBuilder
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(AppHostBuilder));
    private readonly SHA256 _sha256 = SHA256.Create();

    private async Task<string> GetBuildFingerprintAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        _ = logger;

        var msBuildResult = await runner.GetProjectItemsAndPropertiesAsync(
            projectFile,
            ["ProjectReference", "PackageReference", "Compile"],
            ["OutputPath"],
            new DotNetCliRunnerInvocationOptions(),
            cancellationToken
            );

        var json = msBuildResult.Output?.RootElement.ToString();

        var jsonBytes = Encoding.UTF8.GetBytes(json!);
        var hash = _sha256.ComputeHash(jsonBytes);
        var hashString = Convert.ToHexString(hash);
 
        return hashString;
    }

    private string GetAppHostStateBasePath(FileInfo projectFile)
    {
        var fullPath = projectFile.FullName;
        var fullPathBytes = Encoding.UTF8.GetBytes(fullPath);
        var hash = _sha256.ComputeHash(fullPathBytes);
        var hashString = Convert.ToHexString(hash);

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appHostStatePath = Path.Combine(homeDirectory, ".aspire", "apphosts", hashString);
        
        if (Directory.Exists(appHostStatePath))
        {
            return appHostStatePath;
        }
        else
        {
            Directory.CreateDirectory(appHostStatePath);
            return appHostStatePath;
        }
    }

    public async Task<int> BuildAppHostAsync(FileInfo projectFile, bool useCache, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var currentFingerprint = await GetBuildFingerprintAsync(projectFile, cancellationToken);
        var appHostStatePath = GetAppHostStateBasePath(projectFile);
        var buildFingerprintFile = Path.Combine(appHostStatePath, "fingerprint.txt");

        if (File.Exists(buildFingerprintFile) && useCache)
        {
            var lastFingerprint = await File.ReadAllTextAsync(buildFingerprintFile, cancellationToken);
            if (lastFingerprint == currentFingerprint)
            {
                return 0;
            }
        }

        var exitCode = await runner.BuildAsync(projectFile, options, cancellationToken);

        await File.WriteAllTextAsync(buildFingerprintFile, currentFingerprint, cancellationToken);

        return exitCode;
    }
}