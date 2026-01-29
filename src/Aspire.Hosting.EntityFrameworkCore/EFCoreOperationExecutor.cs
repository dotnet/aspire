// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;

namespace Aspire.Hosting;

/// <summary>
/// Executes EF Core design-time operations against a project using reflection.
/// </summary>
/// <remarks>
/// This class uses reflection to load Microsoft.EntityFrameworkCore.Design from the target project
/// and invoke its OperationExecutor to execute design-time operations. The target project must reference
/// Microsoft.EntityFrameworkCore.Design package for these operations to work.
/// </remarks>
internal sealed class EFCoreOperationExecutor : IDisposable
{
    private const string DesignAssemblyName = "Microsoft.EntityFrameworkCore.Design";
    private const string ExecutorTypeName = "Microsoft.EntityFrameworkCore.Design.OperationExecutor";
    private const string ReportHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationReportHandler";
    private const string ResultHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationResultHandler";

    private readonly ProjectResource _startupProjectResource;
    private readonly ProjectResource? _targetProjectResource;
    private readonly string? _contextTypeName;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

    private string? _startupAssemblyPath;
    private string? _targetAssemblyPath;
    private string? _projectDirectory;
    private string? _rootNamespace;
    private string? _designAssemblyPath;
    private string? _framework;
    private string? _configuration;
    private string? _runtime;
    private bool? _nullable;
    private bool _initialized;

    public EFCoreOperationExecutor(
        ProjectResource startupProjectResource,
        ProjectResource? targetProjectResource,
        string? contextTypeName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _startupProjectResource = startupProjectResource;
        _targetProjectResource = targetProjectResource;
        _contextTypeName = contextTypeName;
        _logger = logger;
        _cancellationToken = cancellationToken;

        // Get build settings from the entry assembly (AppHost)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly?.Location is { } appHostAssemblyPath && !string.IsNullOrEmpty(appHostAssemblyPath))
        {
            // Get configuration from assembly attribute
            var configAttribute = entryAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configAttribute?.Configuration is { } configuration)
            {
                _configuration = configuration;
            }

            // Get target framework from assembly attribute
            var frameworkAttribute = entryAssembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            if (frameworkAttribute?.FrameworkName is { } frameworkName)
            {
                // FrameworkName is in format ".NETCoreApp,Version=v8.0" - extract the version part
                const string versionPrefix = ".NETCoreApp,Version=v";
                var versionIndex = frameworkName.IndexOf(versionPrefix, StringComparison.OrdinalIgnoreCase);
                if (versionIndex >= 0)
                {
                    var version = frameworkName.Substring(versionIndex + versionPrefix.Length);
                    _framework = $"net{version}";
                }
            }

            // Parse configuration, framework and runtime from the assembly path (if not found from attributes)
            ParseBuildSettingsFromPath(appHostAssemblyPath);
        }
    }

    private async Task<EFOperationResult> EnsurePathsInitializedAsync()
    {
        if (_initialized)
        {
            return _startupAssemblyPath != null
                ? new EFOperationResult { Success = true }
                : new EFOperationResult { Success = false, ErrorMessage = "Could not find compiled assembly for project." };
        }

        _initialized = true;

        var startupProjectPath = GetProjectPath(_startupProjectResource);
        if (startupProjectPath == null)
        {
            return new EFOperationResult { Success = false, ErrorMessage = "Could not determine startup project path. Ensure the project has an IProjectMetadata annotation." };
        }

        var startupProps = await GetProjectPropertiesAsync(startupProjectPath).ConfigureAwait(false);
        _startupAssemblyPath = startupProps.TargetPath;

        if (_startupAssemblyPath == null || !File.Exists(_startupAssemblyPath))
        {
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = $"Could not find compiled assembly for startup project. Ensure the project is built. Expected at: {_startupAssemblyPath ?? "(unknown)"}"
            };
        }

        if (_targetProjectResource != null)
        {
            var targetProjectPath = GetProjectPath(_targetProjectResource);
            if (targetProjectPath == null)
            {
                return new EFOperationResult { Success = false, ErrorMessage = "Could not determine target project path. Ensure the project has an IProjectMetadata annotation." };
            }

            var targetProps = await GetProjectPropertiesAsync(targetProjectPath).ConfigureAwait(false);

            _projectDirectory = targetProps.ProjectDir ?? Path.GetDirectoryName(targetProjectPath)!;
            _targetAssemblyPath = targetProps.TargetPath;
            _rootNamespace = targetProps.RootNamespace;
            _nullable = string.Equals(targetProps.Nullable, "enable", StringComparison.OrdinalIgnoreCase);

            // Look for design assembly in target project first, then startup project
            _designAssemblyPath = targetProps.DesignAssemblyPath ?? startupProps.DesignAssemblyPath;

            if (_targetAssemblyPath == null || !File.Exists(_targetAssemblyPath))
            {
                return new EFOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Could not find compiled assembly for target project. Ensure the project is built. Expected at: {_targetAssemblyPath ?? "(unknown)"}"
                };
            }
        }
        else
        {
            _projectDirectory = startupProps.ProjectDir ?? Path.GetDirectoryName(startupProjectPath)!;
            _targetAssemblyPath = _startupAssemblyPath;
            _rootNamespace = startupProps.RootNamespace;
            _nullable = string.Equals(startupProps.Nullable, "enable", StringComparison.OrdinalIgnoreCase);
            _designAssemblyPath = startupProps.DesignAssemblyPath;
        }

        // Fall back to assembly name if RootNamespace not found
        _rootNamespace ??= Path.GetFileNameWithoutExtension(_targetAssemblyPath);

        return new EFOperationResult { Success = true };
    }

    private record ProjectProperties(
        string? TargetPath,
        string? RootNamespace,
        string? ProjectDir,
        string? OutputPath,
        string? DesignAssemblyPath,
        string? Nullable);

    /// <summary>
    /// Record for deserializing MSBuild JSON output.
    /// </summary>
    private sealed record MSBuildOutput
    {
        public Dictionary<string, string>? Properties { get; init; }
        public Dictionary<string, Dictionary<string, string>[]>? Items { get; init; }
    }

    private async Task<ProjectProperties> GetProjectPropertiesAsync(string projectPath)
    {
        // Build MSBuild arguments similar to how EF Core does it
        // Use /t:ResolvePackageAssets to ensure RuntimeCopyLocalItems are available
        var args = new List<string>
        {
            "/getProperty:TargetPath",
            "/getProperty:RootNamespace",
            "/getProperty:ProjectDir",
            "/getProperty:OutputPath",
            "/getProperty:Nullable",
            "/t:ResolvePackageAssets",
            "/getItem:RuntimeCopyLocalItems"
        };

        var result = await RunMSBuildAsync(string.Join(" ", args), projectPath).ConfigureAwait(false);

        if (string.IsNullOrEmpty(result))
        {
            throw new InvalidOperationException($"MSBuild returned empty output for project '{projectPath}'. Ensure the project is built.");
        }

        // Parse JSON output
        MSBuildOutput? msbuildOutput;
        try
        {
            msbuildOutput = JsonSerializer.Deserialize<MSBuildOutput>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse MSBuild JSON output: {Output}", result);
            throw new InvalidOperationException($"Failed to parse MSBuild output as JSON. Output: {result}", ex);
        }

        if (msbuildOutput?.Properties == null)
        {
            throw new InvalidOperationException($"MSBuild output did not contain expected Properties. Output: {result}");
        }

        var properties = msbuildOutput.Properties;

        properties.TryGetValue("TargetPath", out var targetPath);
        properties.TryGetValue("RootNamespace", out var rootNamespace);
        properties.TryGetValue("ProjectDir", out var projectDir);
        properties.TryGetValue("OutputPath", out var outputPath);
        properties.TryGetValue("Nullable", out var nullable);

        string? designAssemblyPath = null;
        if (msbuildOutput.Items?.TryGetValue("RuntimeCopyLocalItems", out var runtimeItems) == true && runtimeItems != null)
        {
            designAssemblyPath = runtimeItems
                .Select(item => item.TryGetValue("FullPath", out var fullPath) ? fullPath : null)
                .FirstOrDefault(path => path != null && path.Contains(DesignAssemblyName, StringComparison.OrdinalIgnoreCase));
        }

        _logger.LogDebug(
            "Project properties for '{ProjectPath}': TargetPath={TargetPath}, RootNamespace={RootNamespace}, " +
            "ProjectDir={ProjectDir}, OutputPath={OutputPath}, Nullable={Nullable}, DesignAssemblyPath={DesignAssemblyPath}",
            projectPath, targetPath, rootNamespace, projectDir, outputPath, nullable, designAssemblyPath);

        return new ProjectProperties(targetPath, rootNamespace, projectDir, outputPath, designAssemblyPath, nullable);
    }

    private async Task<string> RunMSBuildAsync(string arguments, string projectPath)
    {
        // Construct MSBuild arguments with build configuration heuristics
        var msbuildArgs = BuildMSBuildArguments(arguments, projectPath);

        _logger.LogDebug("Running MSBuild: dotnet {Arguments}", msbuildArgs);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = msbuildArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet msbuild process.");
        }

        var output = await process.StandardOutput.ReadToEndAsync(_cancellationToken).ConfigureAwait(false);
        var errorOutput = await process.StandardError.ReadToEndAsync(_cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(_cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            _logger.LogDebug("dotnet msbuild failed with exit code {ExitCode}: {Error}", process.ExitCode, errorOutput);
            throw new InvalidOperationException($"MSBuild failed with exit code {process.ExitCode}: {errorOutput}");
        }

        return output.Trim();
    }

    /// <summary>
    /// Builds MSBuild arguments using stored framework, configuration, and runtime.
    /// </summary>
    private string BuildMSBuildArguments(string arguments, string projectPath)
    {
        var args = $"msbuild {arguments} \"{projectPath}\"";

        if (_framework != null)
        {
            args += $" /p:TargetFramework={_framework}";
        }

        if (_configuration != null)
        {
            args += $" /p:Configuration={_configuration}";
        }

        if (_runtime != null)
        {
            args += $" /p:RuntimeIdentifier={_runtime}";
        }

        return args;
    }

    /// <summary>
    /// Parses framework, configuration, and runtime from an assembly path.
    /// Typical path patterns:
    ///   bin/{Configuration}/{Framework}/{AssemblyName}.dll
    ///   bin/{Configuration}/{Framework}/{Runtime}/{AssemblyName}.dll
    ///   bin/{AssemblyName}/{Configuration}/{Framework}/{AssemblyName}.dll
    ///   bin/{AssemblyName}/{Configuration}/{Framework}/{Runtime}/{AssemblyName}.dll
    /// </summary>
    private void ParseBuildSettingsFromPath(string assemblyPath)
    {
        // Split the path into segments
        var segments = assemblyPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Find the "bin" segment as an anchor point
        var binIndex = -1;
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                binIndex = i;
                break;
            }
        }

        if (binIndex < 0 || binIndex + 2 >= segments.Length)
        {
            _logger.LogDebug("Could not parse build settings from assembly path: {AssemblyPath}", assemblyPath);
            return;
        }

        // Check if the first segment after "bin" is a configuration or assembly name
        // If it's a configuration (Debug/Release), use standard layout
        // Otherwise, assume it's assembly name and configuration is next
        var firstSegment = segments[binIndex + 1];
        var isStandardLayout = firstSegment.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                               firstSegment.Equals("Release", StringComparison.OrdinalIgnoreCase);

        int configIndex, frameworkIndex;

        if (isStandardLayout)
        {
            // Pattern: bin/{Configuration}/{Framework}/{Runtime?}/{AssemblyName}.dll
            configIndex = binIndex + 1;
            frameworkIndex = binIndex + 2;
        }
        else
        {
            // Pattern: bin/{AssemblyName}/{Configuration}/{Framework}/{Runtime?}/{AssemblyName}.dll
            configIndex = binIndex + 2;
            frameworkIndex = binIndex + 3;

            if (frameworkIndex >= segments.Length)
            {
                _logger.LogDebug("Could not parse build settings from assembly path: {AssemblyPath}", assemblyPath);
                return;
            }
        }

        // Parse configuration
        if (_configuration == null
            && configIndex < segments.Length)
        {
            var configCandidate = segments[configIndex];
            if (configCandidate.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                configCandidate.Equals("Release", StringComparison.OrdinalIgnoreCase))
            {
                _configuration = configCandidate;
            }
        }

        // Parse framework (e.g., net8.0, net9.0)
        if (_framework == null
            && frameworkIndex < segments.Length)
        {
            var frameworkCandidate = segments[frameworkIndex];
            if (frameworkCandidate.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                _framework = frameworkCandidate;
            }
        }

        // Parse runtime if present (e.g., win-x64, linux-arm64)
        // Runtime comes after framework and before the assembly name
        var runtimeIndex = frameworkIndex + 1;
        if (_runtime == null
            && runtimeIndex < segments.Length - 1)
        {
            var runtimeCandidate = segments[runtimeIndex];
            // Runtime identifiers typically contain a hyphen (win-x64, linux-arm64, osx-x64, etc.)
            if (runtimeCandidate.Contains('-') && !runtimeCandidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                _runtime = runtimeCandidate;
            }
        }

        _logger.LogDebug(
            "Parsed build settings from path: Framework={Framework}, Configuration={Configuration}, Runtime={Runtime}",
            _framework, _configuration, _runtime);
    }

    private static string? GetProjectPath(ProjectResource projectResource)
    {
        if (projectResource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            return metadata.ProjectPath;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<EFOperationResult> ExecuteInIsolatedContextAsync(
        Func<object, Assembly, Type, object?> operation,
        bool handleDatabaseNotFound = false)
    {
        var initResult = await EnsurePathsInitializedAsync().ConfigureAwait(false);
        if (!initResult.Success)
        {
            return initResult;
        }

        var targetBasePath = Path.GetDirectoryName(_targetAssemblyPath)!;
        var startupBasePath = Path.GetDirectoryName(_startupAssemblyPath)!;

        return ExecuteOperationInContext(
            targetBasePath,
            startupBasePath,
            operation,
            handleDatabaseNotFound);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private EFOperationResult ExecuteOperationInContext(
        string targetBasePath,
        string startupBasePath,
        Func<object, Assembly, Type, object?> operation,
        bool handleDatabaseNotFound)
    {
        if (string.IsNullOrEmpty(_designAssemblyPath) || !File.Exists(_designAssemblyPath))
        {
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = "Microsoft.EntityFrameworkCore.Design assembly not found. Ensure the target project references Microsoft.EntityFrameworkCore.Design."
            };
        }

        var alc = new EFCoreDesignLoadContext(targetBasePath, startupBasePath, _targetAssemblyPath!, _startupAssemblyPath);

        try
        {
            var commandsAssembly = alc.LoadFromAssemblyPath(_designAssemblyPath);

            // Load the target and startup assemblies in the same context
            alc.LoadFromAssemblyPath(_targetAssemblyPath!);
            if (_startupAssemblyPath != _targetAssemblyPath)
            {
                alc.LoadFromAssemblyPath(_startupAssemblyPath!);
            }

            // Enter the custom context so Assembly.Load() calls resolve to this context
            using var contextScope = alc.EnterContextualReflection();

            // Create report handler
            var reportHandlerType = commandsAssembly.GetType(ReportHandlerTypeName, throwOnError: true, ignoreCase: false)!;
            var reportHandler = Activator.CreateInstance(
                reportHandlerType,
                (Action<string>)(msg => _logger.LogError("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogWarning("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogInformation("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogDebug("[EF] {Message}", msg)))!;

            // Create executor with full paths
            var executorType = commandsAssembly.GetType(ExecutorTypeName, throwOnError: true, ignoreCase: false)!;
            var executor = Activator.CreateInstance(
                executorType,
                reportHandler,
                new Dictionary<string, object?>
                {
                    { "targetName", Path.GetFileNameWithoutExtension(_targetAssemblyPath) },
                    { "startupTargetName", Path.GetFileNameWithoutExtension(_startupAssemblyPath) },
                    { "projectDir", _projectDirectory },
                    { "rootNamespace", _rootNamespace },
                    { "language", "C#" },
                    { "nullable", _nullable ?? false },
                    { "remainingArguments", Array.Empty<string>() }
                })!;

            var resultHandlerType = commandsAssembly.GetType(ResultHandlerTypeName, throwOnError: true, ignoreCase: false)!;

            var operationResult = operation(executor, commandsAssembly, resultHandlerType);

            return new EFOperationResult
            {
                Success = true,
                Output = operationResult?.ToString()
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Microsoft.EntityFrameworkCore.Design assembly not found. Ensure the target project references Microsoft.EntityFrameworkCore.Design.");
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = "Microsoft.EntityFrameworkCore.Design assembly not found. Ensure the target project references Microsoft.EntityFrameworkCore.Design."
            };
        }
        catch (Exception ex)
        {
            var errorMessage = GetInnerExceptionMessage(ex);

            if (handleDatabaseNotFound &&
                (errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                 errorMessage.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)))
            {
                return new EFOperationResult
                {
                    Success = true,
                    Output = "Database has not been created yet.\nRun 'Update Database' to create and apply migrations."
                };
            }

            _logger.LogError(ex, "Failed to execute EF Core operation.");
            return new EFOperationResult { Success = false, ErrorMessage = errorMessage };
        }
        finally
        {
            alc.Unload();
        }
    }

    private sealed class EFCoreDesignLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly AssemblyDependencyResolver? _startupResolver;
        private readonly string _targetBasePath;
        private readonly string _startupBasePath;

        public EFCoreDesignLoadContext(string targetBasePath, string startupBasePath, string targetAssemblyPath, string? startupAssemblyPath)
            : base(isCollectible: true)
        {
            _targetBasePath = targetBasePath;
            _startupBasePath = startupBasePath;
            _resolver = new AssemblyDependencyResolver(targetAssemblyPath);
            if (startupAssemblyPath != null && startupAssemblyPath != targetAssemblyPath)
            {
                _startupResolver = new AssemblyDependencyResolver(startupAssemblyPath);
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // First try to resolve using the main dependency resolver
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Try the startup resolver if available
            if (_startupResolver != null)
            {
                assemblyPath = _startupResolver.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }
            }

            // Fall back to looking in the target base path
            foreach (var extension in new[] { ".dll", ".exe" })
            {
                var path = Path.Combine(_targetBasePath, assemblyName.Name + extension);
                if (File.Exists(path))
                {
                    return LoadFromAssemblyPath(path);
                }
            }

            // Fall back to looking in the startup base path
            if (_startupBasePath != _targetBasePath)
            {
                foreach (var extension in new[] { ".dll", ".exe" })
                {
                    var path = Path.Combine(_startupBasePath, assemblyName.Name + extension);
                    if (File.Exists(path))
                    {
                        return LoadFromAssemblyPath(path);
                    }
                }
            }

            // Let the default context try to load it
            return base.Load(assemblyName);
        }
    }

    public async Task<EFOperationResult> UpdateDatabaseAsync()
    {
        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            InvokeOperation(executor, commandsAssembly, resultHandlerType, "UpdateDatabase", new Dictionary<string, object?>
            {
                { "targetMigration", null },
                { "connectionString", null },
                { "contextType", _contextTypeName }
            });
            return null;
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> DropDatabaseAsync()
    {
        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            InvokeOperation(executor, commandsAssembly, resultHandlerType, "DropDatabase", new Dictionary<string, object?>
            {
                { "contextType", _contextTypeName }
            });
            return null;
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> ResetDatabaseAsync()
    {
        // First drop the database
        var dropResult = await DropDatabaseAsync().ConfigureAwait(false);
        if (!dropResult.Success)
        {
            // If drop fails because database doesn't exist, continue with update
            if (!dropResult.ErrorMessage?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return dropResult;
            }
        }

        // Then update (recreate with migrations)
        return await UpdateDatabaseAsync().ConfigureAwait(false);
    }

    public async Task<EFOperationResult> AddMigrationAsync(string? migrationName = null, string? outputDir = null, string? @namespace = null)
    {
        migrationName ??= $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogInformation("Creating migration with name: {MigrationName}", migrationName);

        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            var result = InvokeOperation<IDictionary>(executor, commandsAssembly, resultHandlerType, "AddMigration", new Dictionary<string, object?>
            {
                { "name", migrationName },
                { "outputDir", outputDir },
                { "contextType", _contextTypeName },
                { "namespace", @namespace }
            });

            return result?["MigrationFile"]?.ToString();
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> RemoveMigrationAsync()
    {
        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            InvokeOperation<IDictionary>(executor, commandsAssembly, resultHandlerType, "RemoveMigration", new Dictionary<string, object?>
            {
                { "contextType", _contextTypeName },
                { "force", true }
            });
            return null;
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GetDatabaseStatusAsync()
    {
        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            var migrations = InvokeOperation<IEnumerable<IDictionary>>(executor, commandsAssembly, resultHandlerType, "GetMigrations", new Dictionary<string, object?>
            {
                { "contextType", _contextTypeName },
                { "connectionString", null },
                { "noConnect", false }
            });

            var statusLines = new List<string>();
            string? lastAppliedMigration = null;
            string? lastMigration = null;
            var pendingMigrations = new List<string>();

            if (migrations != null)
            {
                foreach (var migration in migrations)
                {
                    var id = migration["Id"]?.ToString() ?? "Unknown";
                    var applied = migration["Applied"] as bool? ?? false;
                    lastMigration = id;

                    if (applied)
                    {
                        lastAppliedMigration = id;
                        statusLines.Add($"  [Applied] {id}");
                    }
                    else
                    {
                        pendingMigrations.Add(id);
                        statusLines.Add($"  [Pending] {id}");
                    }
                }
            }

            var hasPendingModelChanges = false;
            try
            {
                InvokeOperation(executor, commandsAssembly, resultHandlerType, "HasPendingModelChanges", new Dictionary<string, object?>
                {
                    { "contextType", _contextTypeName }
                });
            }
            catch
            {
                hasPendingModelChanges = true;
            }

            var summary = new List<string>
            {
                $"Current Applied Migration: {lastAppliedMigration ?? "None (database not created)"}.",
                $"Latest Migration: {lastMigration ?? "None"}.",
                $"Pending Migrations: {(pendingMigrations.Count > 0 ? string.Join(", ", pendingMigrations) : "None")}.",
                $"Has Pending Model Changes: {(hasPendingModelChanges ? "Yes" : "No")}."
            };

            if (statusLines.Count > 0)
            {
                summary.Add("");
                summary.Add("Migration History:");
                summary.AddRange(statusLines);
            }

            return string.Join(Environment.NewLine, summary);
        }, handleDatabaseNotFound: true).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GenerateMigrationScriptAsync(string? outputPath = null)
    {
        return await ExecuteInIsolatedContextAsync((executor, commandsAssembly, resultHandlerType) =>
        {
            var script = InvokeOperation<string>(executor, commandsAssembly, resultHandlerType, "ScriptMigration", new Dictionary<string, object?>
            {
                { "fromMigration", null },
                { "toMigration", null },
                { "idempotent", true },
                { "noTransactions", false },
                { "contextType", _contextTypeName }
            });

            if (outputPath != null && script != null)
            {
                File.WriteAllText(outputPath, script);
            }

            return script;
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GenerateMigrationBundleAsync(string? outputPath = null)
    {
        // Note: Migration bundles require using dotnet ef migrations bundle command
        // as there's no direct API for this in OperationExecutor
        var initResult = await EnsurePathsInitializedAsync().ConfigureAwait(false);
        if (!initResult.Success)
        {
            return initResult;
        }

        _ = outputPath; // Unused - included for API consistency
        _logger.LogWarning("Migration bundle generation is not yet implemented via reflection. Use 'dotnet ef migrations bundle' from the command line.");
        return new EFOperationResult
        {
            Success = false,
            ErrorMessage = "Migration bundle generation requires the dotnet ef CLI. Use 'dotnet ef migrations bundle' from the command line."
        };
    }

    private static void InvokeOperation(object executor, Assembly commandsAssembly, Type resultHandlerType, string operationName, IDictionary arguments)
    {
        InvokeOperationImpl(executor, commandsAssembly, resultHandlerType, operationName, arguments);
    }

    private static TResult InvokeOperation<TResult>(object executor, Assembly commandsAssembly, Type resultHandlerType, string operationName, IDictionary arguments)
    {
        return (TResult)InvokeOperationImpl(executor, commandsAssembly, resultHandlerType, operationName, arguments)!;
    }

    private static object? InvokeOperationImpl(object executor, Assembly commandsAssembly, Type resultHandlerType, string operationName, IDictionary arguments)
    {
        dynamic resultHandler = Activator.CreateInstance(resultHandlerType)!;

        // Execute the operation by creating the nested operation type
        var operationType = commandsAssembly.GetType($"{ExecutorTypeName}+{operationName}", throwOnError: true, ignoreCase: true)!;
        Activator.CreateInstance(operationType, executor, resultHandler, arguments);

        if (resultHandler.ErrorType != null)
        {
            throw new InvalidOperationException(resultHandler.ErrorMessage);
        }

        return resultHandler.Result;
    }

    private static string GetInnerExceptionMessage(Exception ex)
    {
        var innermost = ex;
        while (innermost.InnerException != null)
        {
            innermost = innermost.InnerException;
        }
        return innermost.Message;
    }

    public void Dispose()
    {
        // Nothing to dispose - context is unloaded after each operation
    }
}

/// <summary>
/// Result of an EF Core operation.
/// </summary>
internal sealed class EFOperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Output { get; init; }
}
