// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using MsalLogger = Microsoft.Extensions.Logging.ILogger;
using NuGetLogger = NuGet.Common.ILogger;
using NuGetLogLevel = NuGet.Common.LogLevel;
using NuGetLogMessage = NuGet.Common.ILogMessage;

namespace Aspire.Hosting.PackageManagement;

internal sealed class PackageExecutableResolver(ILogger<PackageExecutableResolver> logger) : IPackageExecutableResolver
{
    private const string DefaultTargetFramework = "net10.0";
    private const string NuGetOrgUrl = "https://api.nuget.org/v3/index.json";

    public async Task<PackageExecutableResolutionResult> ResolveAsync(PackageExecutableResource resource, CancellationToken cancellationToken)
    {
        var configuration = resource.PackageConfiguration ?? throw new DistributedApplicationException($"The package executable resource '{resource.Name}' is missing package configuration.");

        var version = configuration.Version;
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DistributedApplicationException($"Package executable resource '{resource.Name}' requires an explicit package version. Call WithPackageVersion(...) before starting the resource.");
        }

        var appHostDirectory = resource.WorkingDirectory;
        var settings = Settings.LoadDefaultSettings(appHostDirectory);
        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
        var normalizedVersion = NuGetVersion.Parse(version).ToNormalizedString();
        var packageDirectory = Path.Combine(globalPackagesFolder, configuration.PackageId.ToLowerInvariant(), normalizedVersion);

        if (!Directory.Exists(packageDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RestorePackageAsync(configuration, settings, globalPackagesFolder, cancellationToken).ConfigureAwait(false);
        }

        if (!Directory.Exists(packageDirectory))
        {
            throw new DistributedApplicationException($"Unable to restore package '{configuration.PackageId}' version '{configuration.Version}' for resource '{resource.Name}'.");
        }

        var executablePath = ResolveExecutablePath(packageDirectory, configuration.ExecutableName, configuration.PackageId);
        var workingDirectory = ResolveWorkingDirectory(packageDirectory, executablePath, configuration.WorkingDirectory, configuration.PackageId);
        var command = executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? "dotnet" : executablePath;
        var arguments = executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? [executablePath]
            : Array.Empty<string>();

        return new PackageExecutableResolutionResult
        {
            PackageId = configuration.PackageId,
            PackageVersion = normalizedVersion,
            PackageDirectory = packageDirectory,
            ExecutablePath = executablePath,
            Command = command,
            WorkingDirectory = workingDirectory,
            Arguments = arguments
        };
    }

    private async Task RestorePackageAsync(PackageExecutableAnnotation configuration, ISettings settings, string globalPackagesFolder, CancellationToken cancellationToken)
    {
        var packageSources = LoadPackageSources(configuration, settings);
        var framework = NuGetFramework.Parse(DefaultTargetFramework);
        var outputPath = Path.Combine(Path.GetTempPath(), "aspire-package-executables", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(outputPath);

        try
        {
            var restoreAttempts = BuildRestoreAttempts(packageSources, configuration.IgnoreFailedSources);
            List<string>? errors = null;

            foreach (var sources in restoreAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var summary = await ExecuteRestoreAsync(configuration, framework, outputPath, globalPackagesFolder, sources).ConfigureAwait(false);
                if (summary is { Success: true })
                {
                    return;
                }

                errors ??= [];
                errors.AddRange(summary?.Errors?.Select(error => error.Message) ?? ["Unknown restore error"]);
            }

            throw new DistributedApplicationException($"Package restore failed for '{configuration.PackageId}' version '{configuration.Version}': {string.Join(Environment.NewLine, errors ?? ["Unknown restore error"])}");
        }
        finally
        {
            try
            {
                Directory.Delete(outputPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private async Task<RestoreSummary?> ExecuteRestoreAsync(PackageExecutableAnnotation configuration, NuGetFramework framework, string outputPath, string globalPackagesFolder, IReadOnlyList<PackageSource> packageSources)
    {
        var packageSpec = BuildPackageSpec(configuration, framework, outputPath, globalPackagesFolder, packageSources);
        var dependencyGraphSpec = new DependencyGraphSpec();
        dependencyGraphSpec.AddProject(packageSpec);
        dependencyGraphSpec.AddRestore(packageSpec.RestoreMetadata.ProjectUniqueName);

        var providerCache = new RestoreCommandProvidersCache();
        var providers = new List<IPreLoadedRestoreRequestProvider>
        {
            new DependencyGraphSpecRequestProvider(providerCache, dependencyGraphSpec)
        };

        using var cacheContext = new SourceCacheContext();
        var restoreContext = new RestoreArgs
        {
            CacheContext = cacheContext,
            Log = new NuGetCommonLogger(logger),
            PreLoadedRequestProviders = providers,
            DisableParallel = Environment.ProcessorCount == 1,
            AllowNoOp = false,
            GlobalPackagesFolder = globalPackagesFolder,
        };

        var results = await RestoreRunner.RunAsync(restoreContext).ConfigureAwait(false);
        return results.Count > 0 ? results[0] : null;
    }

    private static PackageSpec BuildPackageSpec(PackageExecutableAnnotation configuration, NuGetFramework framework, string outputPath, string packagesPath, IReadOnlyList<PackageSource> sources)
    {
        var version = configuration.Version!;
        var projectName = "AspirePackageExecutableRestore";
        var projectPath = Path.Combine(outputPath, "project.json");
        var targetFrameworkName = framework.GetShortFolderName();

        var dependency = new LibraryDependency
        {
            LibraryRange = new LibraryRange(
                configuration.PackageId,
                VersionRange.Parse(version),
                LibraryDependencyTarget.Package)
        };

        var targetFramework = new TargetFrameworkInformation
        {
            FrameworkName = framework,
            TargetAlias = targetFrameworkName,
            Dependencies = ImmutableArray.Create(dependency)
        };

        var restoreMetadata = new ProjectRestoreMetadata
        {
            ProjectUniqueName = projectName,
            ProjectName = projectName,
            ProjectPath = projectPath,
            ProjectStyle = ProjectStyle.PackageReference,
            OutputPath = outputPath,
            PackagesPath = packagesPath,
            OriginalTargetFrameworks = [targetFrameworkName],
        };

        foreach (var source in sources)
        {
            restoreMetadata.Sources.Add(source);
        }

        restoreMetadata.TargetFrameworks.Add(new ProjectRestoreMetadataFrameworkInfo(framework)
        {
            TargetAlias = targetFrameworkName
        });

        return new PackageSpec([targetFramework])
        {
            Name = projectName,
            FilePath = projectPath,
            RestoreMetadata = restoreMetadata,
        };
    }

    private static IReadOnlyList<PackageSource> LoadPackageSources(PackageExecutableAnnotation configuration, ISettings settings)
    {
        var packageSources = new List<PackageSource>();

        foreach (var source in configuration.Sources)
        {
            packageSources.Add(new PackageSource(source));
        }

        if (!configuration.IgnoreExistingFeeds)
        {
            var provider = new PackageSourceProvider(settings);

            foreach (var source in provider.LoadPackageSources())
            {
                if (source.IsEnabled && !packageSources.Any(existingSource => string.Equals(existingSource.Source, source.Source, StringComparison.OrdinalIgnoreCase)))
                {
                    packageSources.Add(source);
                }
            }
        }

        if (!packageSources.Any(source => string.Equals(source.Source, NuGetOrgUrl, StringComparison.OrdinalIgnoreCase)))
        {
            packageSources.Add(new PackageSource(NuGetOrgUrl, "nuget.org"));
        }

        return packageSources;
    }

    internal static IReadOnlyList<IReadOnlyList<PackageSource>> BuildRestoreAttempts(IReadOnlyList<PackageSource> packageSources, bool ignoreFailedSources)
    {
        ArgumentNullException.ThrowIfNull(packageSources);

        if (!ignoreFailedSources || packageSources.Count <= 1)
        {
            return [packageSources];
        }

        var attempts = new List<IReadOnlyList<PackageSource>>(capacity: packageSources.Count + 1)
        {
            packageSources
        };

        foreach (var source in packageSources)
        {
            attempts.Add([source]);
        }

        return attempts;
    }

    private static string ResolveExecutablePath(string packageDirectory, string? executableName, string packageId)
    {
        var candidateRoots = GetCandidateRoots(packageDirectory);
        if (candidateRoots.Count == 0)
        {
            throw new DistributedApplicationException($"Package '{packageId}' does not contain a supported executable layout. Package executable resources currently expect runnable assets under the package lib or tools folders.");
        }

        var candidates = candidateRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(IsSupportedExecutableCandidate)
            .Where(path => MatchesExecutableName(path, executableName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var searchLocations = string.Join("', '", candidateRoots);

        return candidates.Length switch
        {
            0 => throw new DistributedApplicationException(executableName is null
                ? $"Unable to locate a runnable executable asset under '{searchLocations}' for package '{packageId}'. Use WithPackageExecutable(...) to select a specific executable file."
                : $"Unable to locate an executable named '{executableName}' under '{searchLocations}' for package '{packageId}'."),
            1 => candidates[0],
            _ => throw new DistributedApplicationException(executableName is null
                ? $"Multiple runnable executable assets were found under '{searchLocations}' for package '{packageId}'. Use WithPackageExecutable(...) to disambiguate."
                : $"Multiple runnable executable assets matched '{executableName}' under '{searchLocations}' for package '{packageId}'.")
        };
    }

    private static IReadOnlyList<string> GetCandidateRoots(string packageDirectory)
    {
        var candidateRoots = new List<string>(capacity: 2);

        var libDirectory = Path.Combine(packageDirectory, "lib");
        if (Directory.Exists(libDirectory))
        {
            candidateRoots.Add(libDirectory);
        }

        var toolsDirectory = Path.Combine(packageDirectory, "tools");
        if (Directory.Exists(toolsDirectory))
        {
            candidateRoots.Add(toolsDirectory);
        }

        return candidateRoots;
    }

    internal static string ResolveWorkingDirectory(string packageDirectory, string executablePath, string? workingDirectory, string packageId)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return Path.GetDirectoryName(executablePath) ?? packageDirectory;
        }

        if (Path.IsPathRooted(workingDirectory))
        {
            throw new DistributedApplicationException($"Package executable resource for package '{packageId}' requires a working directory relative to the restored package contents.");
        }

        var resolvedWorkingDirectory = Path.GetFullPath(Path.Combine(packageDirectory, workingDirectory));
        var packageRoot = EnsureTrailingSeparator(Path.GetFullPath(packageDirectory));
        var resolvedRoot = EnsureTrailingSeparator(resolvedWorkingDirectory);

        if (!resolvedRoot.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new DistributedApplicationException($"Package executable resource for package '{packageId}' requires a working directory that stays within the restored package contents.");
        }

        return resolvedWorkingDirectory;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static bool IsSupportedExecutableCandidate(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(extension);
    }

    private static bool MatchesExecutableName(string path, string? executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return true;
        }

        var fileName = Path.GetFileName(path);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

        return string.Equals(fileName, executableName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileNameWithoutExtension, executableName, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class NuGetCommonLogger(MsalLogger logger) : NuGetLogger
{
    public void Log(NuGetLogLevel level, string data)
    {
        if (!logger.IsEnabled(MapLevel(level)))
        {
            return;
        }

        logger.Log(MapLevel(level), "{Message}", data);
    }

    public void Log(NuGetLogMessage message) => Log(message.Level, message.Message);

    public Task LogAsync(NuGetLogLevel level, string data)
    {
        Log(level, data);
        return Task.CompletedTask;
    }

    public Task LogAsync(NuGetLogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }

    public void LogDebug(string data) => Log(NuGetLogLevel.Debug, data);

    public void LogError(string data) => Log(NuGetLogLevel.Error, data);

    public void LogInformation(string data) => Log(NuGetLogLevel.Information, data);

    public void LogInformationSummary(string data) => Log(NuGetLogLevel.Information, data);

    public void LogMinimal(string data) => Log(NuGetLogLevel.Minimal, data);

    public void LogVerbose(string data) => Log(NuGetLogLevel.Verbose, data);

    public void LogWarning(string data) => Log(NuGetLogLevel.Warning, data);

    private static Microsoft.Extensions.Logging.LogLevel MapLevel(NuGetLogLevel level) => level switch
    {
        NuGetLogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
        NuGetLogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
        NuGetLogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
        NuGetLogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Information,
        NuGetLogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
        NuGetLogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
        _ => Microsoft.Extensions.Logging.LogLevel.Information
    };
}