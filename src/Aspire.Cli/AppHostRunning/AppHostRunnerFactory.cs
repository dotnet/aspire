// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for creating AppHost runners based on the project type.
/// </summary>
internal sealed class AppHostRunnerFactory : IAppHostRunnerFactory
{
    private readonly IEnumerable<IAppHostRunner> _runners;

    public AppHostRunnerFactory(IEnumerable<IAppHostRunner> runners)
    {
        _runners = runners;
    }

    /// <inheritdoc />
    public IAppHostRunner GetRunner(AppHostType type)
    {
        var runner = _runners.FirstOrDefault(r => r.SupportedType == type);

        if (runner is null)
        {
            throw new NotSupportedException($"No runner available for AppHost type '{type}'.");
        }

        return runner;
    }

    /// <inheritdoc />
    public IAppHostRunner? TryGetRunner(FileInfo appHostFile)
    {
        var type = DetectAppHostType(appHostFile);

        if (type is null)
        {
            return null;
        }

        return _runners.FirstOrDefault(r => r.SupportedType == type.Value);
    }

    /// <summary>
    /// Detects the AppHost type from the file.
    /// </summary>
    private static AppHostType? DetectAppHostType(FileInfo appHostFile)
    {
        var extension = appHostFile.Extension.ToLowerInvariant();

        return extension switch
        {
            ".csproj" or ".fsproj" or ".vbproj" => AppHostType.DotNetProject,
            ".cs" => AppHostType.DotNetSingleFile,
            ".ts" => AppHostType.TypeScript,
            ".py" => AppHostType.Python,
            ".json" when appHostFile.Name.Equals("aspire.json", StringComparison.OrdinalIgnoreCase) => DetectFromAspireJson(appHostFile),
            _ => null
        };
    }

    /// <summary>
    /// Detects the AppHost type from an aspire.json file.
    /// </summary>
    private static AppHostType? DetectFromAspireJson(FileInfo aspireJsonFile)
    {
        // Look for sibling apphost.ts or apphost.py files
        var directory = aspireJsonFile.Directory;
        if (directory is null)
        {
            return null;
        }

        if (File.Exists(Path.Combine(directory.FullName, "apphost.ts")))
        {
            return AppHostType.TypeScript;
        }

        if (File.Exists(Path.Combine(directory.FullName, "apphost.py")))
        {
            return AppHostType.Python;
        }

        // TODO: Parse aspire.json to check for language field
        return null;
    }
}
