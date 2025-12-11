// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
namespace Aspire.Hosting;

/// <summary>
/// Executes EF Core design-time operations against a project using reflection.
/// </summary>
/// <remarks>
/// This class uses reflection to load Microsoft.EntityFrameworkCore.Design from the target project
/// and invoke its OperationExecutor to execute design-time operations. The target project must reference
/// Microsoft.EntityFrameworkCore.Design package for these operations to work.
/// Assemblies are loaded into a collectible AssemblyLoadContext and unloaded after operations complete.
/// See: https://github.com/dotnet/efcore/blob/main/src/ef/ReflectionOperationExecutor.cs
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

        // Get startup project path
        var startupProjectPath = GetProjectPath(_startupProjectResource);
        if (startupProjectPath == null)
        {
            return new EFOperationResult { Success = false, ErrorMessage = "Could not determine startup project path. Ensure the project has an IProjectMetadata annotation." };
        }

        _startupAssemblyPath = await GetMSBuildPropertyAsync(startupProjectPath, "TargetPath").ConfigureAwait(false);
        
        if (_startupAssemblyPath == null || !File.Exists(_startupAssemblyPath))
        {
            return new EFOperationResult 
            { 
                Success = false, 
                ErrorMessage = $"Could not find compiled assembly for startup project. Ensure the project is built. Expected at: {_startupAssemblyPath ?? "(unknown)"}" 
            };
        }

        // Get target project path (if different from startup)
        if (_targetProjectResource != null)
        {
            var targetProjectPath = GetProjectPath(_targetProjectResource);
            if (targetProjectPath == null)
            {
                return new EFOperationResult { Success = false, ErrorMessage = "Could not determine target project path. Ensure the project has an IProjectMetadata annotation." };
            }

            _projectDirectory = Path.GetDirectoryName(targetProjectPath)!;
            _targetAssemblyPath = await GetMSBuildPropertyAsync(targetProjectPath, "TargetPath").ConfigureAwait(false);
            _rootNamespace = await GetMSBuildPropertyAsync(targetProjectPath, "RootNamespace").ConfigureAwait(false);
            _designAssemblyPath = await GetDesignAssemblyPathAsync(targetProjectPath, _targetAssemblyPath).ConfigureAwait(false);

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
            _projectDirectory = Path.GetDirectoryName(startupProjectPath)!;
            _targetAssemblyPath = _startupAssemblyPath;
            _rootNamespace = await GetMSBuildPropertyAsync(startupProjectPath, "RootNamespace").ConfigureAwait(false);
            _designAssemblyPath = await GetDesignAssemblyPathAsync(startupProjectPath, _startupAssemblyPath).ConfigureAwait(false);
        }

        // Fall back to assembly name if RootNamespace not found
        _rootNamespace ??= Path.GetFileNameWithoutExtension(_targetAssemblyPath);

        return new EFOperationResult { Success = true };
    }

    private async Task<string?> GetMSBuildPropertyAsync(string projectPath, string propertyName)
    {
        return await RunMSBuildAsync($"-getProperty:{propertyName}", projectPath).ConfigureAwait(false);
    }

    private async Task<string?> GetDesignAssemblyPathAsync(string projectPath, string? assemblyPath)
    {
        // Use MSBuild to get the design assembly path from referenced packages
        var result = await RunMSBuildAsync(
            $"-getItem:ReferencePath -getItemMetadata:NuGetPackageId \"{projectPath}\"", 
            projectPath).ConfigureAwait(false);

        if (result == null)
        {
            return null;
        }

        // Parse MSBuild output to find Microsoft.EntityFrameworkCore.Design reference
        var lines = result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains(DesignAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the path from the MSBuild output
                var path = line.Trim();
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        // Fall back to looking in the output directory
        if (assemblyPath != null)
        {
            var outputDir = Path.GetDirectoryName(assemblyPath);
            if (outputDir != null)
            {
                var designPath = Path.Combine(outputDir, $"{DesignAssemblyName}.dll");
                if (File.Exists(designPath))
                {
                    return designPath;
                }
            }
        }

        return null;
    }

    private async Task<string?> RunMSBuildAsync(string arguments, string projectPath)
    {
        try
        {
            // Construct MSBuild arguments with build configuration heuristics
            var msbuildArgs = BuildMSBuildArguments(arguments, projectPath);

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
                _logger.LogWarning("Failed to start dotnet msbuild process.");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(_cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(_cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync(_cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("dotnet msbuild failed with exit code {ExitCode}: {Error}", process.ExitCode, errorOutput);
                return null;
            }

            var result = output.Trim();
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error running dotnet msbuild");
            return null;
        }
    }

    /// <summary>
    /// Builds MSBuild arguments with heuristics to determine target framework, configuration and runtime.
    /// </summary>
    private static string BuildMSBuildArguments(string arguments, string projectPath)
    {
        var args = $"msbuild {arguments} \"{projectPath}\"";

        // Determine configuration from debugger state
        var configuration = Debugger.IsAttached ? "Debug" : "Release";
        args += $" /p:Configuration={configuration}";

        // Determine runtime identifier based on current platform
        var rid = GetRuntimeIdentifier();
        if (rid != null)
        {
            args += $" /p:RuntimeIdentifier={rid}";
        }

        return args;
    }

    /// <summary>
    /// Gets the runtime identifier for the current platform using heuristics.
    /// </summary>
    private static string? GetRuntimeIdentifier()
    {
        // Try to get from environment first (may be set by build)
        var envRid = Environment.GetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER");
        if (!string.IsNullOrEmpty(envRid))
        {
            return envRid;
        }

        // Determine based on current OS and architecture
        var osArch = RuntimeInformation.OSArchitecture;
        var archSuffix = osArch switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => null
        };

        if (archSuffix == null)
        {
            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"win-{archSuffix}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"linux-{archSuffix}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"osx-{archSuffix}";
        }

        return null;
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

        var appBasePath = Path.GetDirectoryName(_targetAssemblyPath)!;
        
        // Use the design assembly path obtained via MSBuild, or fall back to output directory
        var designAssemblyPath = _designAssemblyPath ?? Path.Combine(appBasePath, $"{DesignAssemblyName}.dll");

        if (!File.Exists(designAssemblyPath))
        {
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = "Microsoft.EntityFrameworkCore.Design assembly not found. Ensure the target project references Microsoft.EntityFrameworkCore.Design."
            };
        }

        // ExecuteOperationInContext uses alc.Unload() to trigger unloading
        return ExecuteOperationInContext(
            designAssemblyPath,
            appBasePath,
            operation,
            handleDatabaseNotFound);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private EFOperationResult ExecuteOperationInContext(
        string designAssemblyPath,
        string appBasePath,
        Func<object, Assembly, Type, object?> operation,
        bool handleDatabaseNotFound)
    {
        var alc = new EFCoreDesignLoadContext(appBasePath, designAssemblyPath, _startupAssemblyPath);

        try
        {
            var commandsAssembly = alc.LoadFromAssemblyPath(designAssemblyPath);

            // Load the target and startup assemblies in the same context
            alc.LoadFromAssemblyPath(_targetAssemblyPath!);
            if (_startupAssemblyPath != _targetAssemblyPath)
            {
                alc.LoadFromAssemblyPath(_startupAssemblyPath!);
            }

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
                    { "targetName", _targetAssemblyPath },
                    { "startupTargetName", _startupAssemblyPath },
                    { "projectDir", _projectDirectory },
                    { "rootNamespace", _rootNamespace },
                    { "language", "C#" },
                    { "nullable", true },
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
        private readonly string _appBasePath;

        public EFCoreDesignLoadContext(string appBasePath, string mainAssemblyPath, string? startupAssemblyPath) 
            : base(isCollectible: true)
        {
            _appBasePath = appBasePath;
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
            if (startupAssemblyPath != null && startupAssemblyPath != mainAssemblyPath)
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

            // Fall back to looking in the app base path
            foreach (var extension in new[] { ".dll", ".exe" })
            {
                var path = Path.Combine(_appBasePath, assemblyName.Name + extension);
                if (File.Exists(path))
                {
                    return LoadFromAssemblyPath(path);
                }
            }

            // Return null to let the default context try to load it
            return null;
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

            bool hasPendingModelChanges = false;
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
                $"Current Applied Migration: {lastAppliedMigration ?? "None (database not created)"}",
                $"Latest Migration: {lastMigration ?? "None"}",
                $"Pending Migrations: {(pendingMigrations.Count > 0 ? string.Join(", ", pendingMigrations) : "None")}",
                $"Has Pending Model Changes: {(hasPendingModelChanges ? "Yes" : "No")}"
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
        return (TResult)InvokeOperationImpl(executor, commandsAssembly, resultHandlerType, operationName, arguments);
    }

    private static object InvokeOperationImpl(object executor, Assembly commandsAssembly, Type resultHandlerType, string operationName, IDictionary arguments)
    {
        var resultHandler = Activator.CreateInstance(resultHandlerType)!;

        // Execute the operation by creating the nested operation type
        var operationType = commandsAssembly.GetType($"{ExecutorTypeName}+{operationName}", throwOnError: true, ignoreCase: true)!;
        Activator.CreateInstance(operationType, executor, resultHandler, arguments);

        // Check for errors
        var errorType = (string?)resultHandlerType.GetProperty("ErrorType")?.GetValue(resultHandler);
        if (errorType != null)
        {
            var errorMessage = (string?)resultHandlerType.GetProperty("ErrorMessage")?.GetValue(resultHandler);
            throw new InvalidOperationException(errorMessage ?? "Unknown error occurred.");
        }

        return resultHandlerType.GetProperty("Result")?.GetValue(resultHandler)!;
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
