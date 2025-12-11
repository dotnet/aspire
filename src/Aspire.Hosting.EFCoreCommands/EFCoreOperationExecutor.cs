// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;
#if NET
using System.Runtime.Loader;
#endif

namespace Aspire.Hosting;

/// <summary>
/// Executes EF Core design-time operations against a project using reflection.
/// </summary>
/// <remarks>
/// This class uses reflection to load Microsoft.EntityFrameworkCore.Design from the target project
/// and invoke its OperationExecutor to execute design-time operations. The target project must reference
/// Microsoft.EntityFrameworkCore.Design package for these operations to work.
/// See: https://github.com/dotnet/efcore/blob/main/src/ef/ReflectionOperationExecutor.cs
/// </remarks>
internal sealed class EFCoreOperationExecutor : IDisposable
{
    private const string DesignAssemblyName = "Microsoft.EntityFrameworkCore.Design";
    private const string ExecutorTypeName = "Microsoft.EntityFrameworkCore.Design.OperationExecutor";
    private const string ReportHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationReportHandler";
    private const string ResultHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationResultHandler";

    private readonly ProjectResource _projectResource;
    private readonly string? _contextTypeName;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    
    private object? _executor;
    private Assembly? _commandsAssembly;
    private Type? _resultHandlerType;
    private bool _initialized;
    private string? _appBasePath;

    public EFCoreOperationExecutor(
        ProjectResource projectResource,
        string? contextTypeName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _projectResource = projectResource;
        _contextTypeName = contextTypeName;
        _logger = logger;
        _cancellationToken = cancellationToken;
    }

    private EFOperationResult EnsureInitialized()
    {
        if (_initialized)
        {
            return _executor != null 
                ? new EFOperationResult { Success = true } 
                : new EFOperationResult { Success = false, ErrorMessage = "EF Core Design assembly not found." };
        }

        _initialized = true;

        var projectPath = GetProjectPath();
        if (projectPath == null)
        {
            return new EFOperationResult { Success = false, ErrorMessage = "Could not determine project path." };
        }

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var assemblyFileName = Path.GetFileNameWithoutExtension(projectPath);
        
        // Try to find the output assembly - look in common output directories
        string? assemblyPath = null;
        var possiblePaths = new[]
        {
            Path.Combine(projectDirectory, "bin", "Debug", "net10.0", $"{assemblyFileName}.dll"),
            Path.Combine(projectDirectory, "bin", "Debug", "net9.0", $"{assemblyFileName}.dll"),
            Path.Combine(projectDirectory, "bin", "Debug", "net8.0", $"{assemblyFileName}.dll"),
            Path.Combine(projectDirectory, "bin", "Release", "net10.0", $"{assemblyFileName}.dll"),
            Path.Combine(projectDirectory, "bin", "Release", "net9.0", $"{assemblyFileName}.dll"),
            Path.Combine(projectDirectory, "bin", "Release", "net8.0", $"{assemblyFileName}.dll"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                assemblyPath = path;
                break;
            }
        }

        if (assemblyPath == null)
        {
            return new EFOperationResult 
            { 
                Success = false, 
                ErrorMessage = $"Could not find compiled assembly for project. Ensure the project is built. Looked in: {string.Join(", ", possiblePaths)}" 
            };
        }

        _appBasePath = Path.GetDirectoryName(assemblyPath)!;

        // Set up assembly resolution
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
            // Load the design assembly
            var designAssemblyPath = Path.Combine(_appBasePath, $"{DesignAssemblyName}.dll");
            
#if NET
            _commandsAssembly = File.Exists(designAssemblyPath)
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(designAssemblyPath)
                : AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(DesignAssemblyName));
#else
            _commandsAssembly = File.Exists(designAssemblyPath)
                ? Assembly.LoadFrom(designAssemblyPath)
                : Assembly.Load(DesignAssemblyName);
#endif

            // Create report handler
            var reportHandlerType = _commandsAssembly.GetType(ReportHandlerTypeName, throwOnError: true, ignoreCase: false)!;
            var reportHandler = Activator.CreateInstance(
                reportHandlerType,
                (Action<string>)(msg => _logger.LogError("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogWarning("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogInformation("[EF] {Message}", msg)),
                (Action<string>)(msg => _logger.LogDebug("[EF] {Message}", msg)))!;

            // Create executor
            var executorType = _commandsAssembly.GetType(ExecutorTypeName, throwOnError: true, ignoreCase: false)!;
            _executor = Activator.CreateInstance(
                executorType,
                reportHandler,
                new Dictionary<string, object?>
                {
                    { "targetName", assemblyFileName },
                    { "startupTargetName", assemblyFileName },
                    { "projectDir", projectDirectory },
                    { "rootNamespace", assemblyFileName },
                    { "language", "C#" },
                    { "nullable", true },
                    { "remainingArguments", Array.Empty<string>() }
                })!;

            _resultHandlerType = _commandsAssembly.GetType(ResultHandlerTypeName, throwOnError: true, ignoreCase: false)!;

            return new EFOperationResult { Success = true };
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
            _logger.LogError(ex, "Failed to initialize EF Core operation executor.");
            return new EFOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public Task<EFOperationResult> UpdateDatabaseAsync()
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            try
            {
                InvokeOperation("UpdateDatabase", new Dictionary<string, object?>
                {
                    { "targetMigration", null },
                    { "connectionString", null },
                    { "contextType", _contextTypeName }
                });

                return new EFOperationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new EFOperationResult { Success = false, ErrorMessage = GetInnerExceptionMessage(ex) };
            }
        }, _cancellationToken);
    }

    public Task<EFOperationResult> DropDatabaseAsync()
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            try
            {
                InvokeOperation("DropDatabase", new Dictionary<string, object?>
                {
                    { "contextType", _contextTypeName }
                });

                return new EFOperationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new EFOperationResult { Success = false, ErrorMessage = GetInnerExceptionMessage(ex) };
            }
        }, _cancellationToken);
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

    public Task<EFOperationResult> AddMigrationAsync(string? migrationName = null)
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            migrationName ??= $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
            _logger.LogInformation("Creating migration with name: {MigrationName}", migrationName);

            try
            {
                var result = InvokeOperation<IDictionary>("AddMigration", new Dictionary<string, object?>
                {
                    { "name", migrationName },
                    { "outputDir", null },
                    { "contextType", _contextTypeName },
                    { "namespace", null }
                });

                return new EFOperationResult 
                { 
                    Success = true, 
                    Output = result?["MigrationFile"]?.ToString() 
                };
            }
            catch (Exception ex)
            {
                return new EFOperationResult { Success = false, ErrorMessage = GetInnerExceptionMessage(ex) };
            }
        }, _cancellationToken);
    }

    public Task<EFOperationResult> RemoveMigrationAsync()
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            try
            {
                InvokeOperation<IDictionary>("RemoveMigration", new Dictionary<string, object?>
                {
                    { "contextType", _contextTypeName },
                    { "force", true }
                });

                return new EFOperationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new EFOperationResult { Success = false, ErrorMessage = GetInnerExceptionMessage(ex) };
            }
        }, _cancellationToken);
    }

    public Task<EFOperationResult> GetDatabaseStatusAsync()
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            try
            {
                var migrations = InvokeOperation<IEnumerable<IDictionary>>("GetMigrations", new Dictionary<string, object?>
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

                // Try to check for pending model changes
                bool hasPendingModelChanges = false;
                try
                {
                    InvokeOperation("HasPendingModelChanges", new Dictionary<string, object?>
                    {
                        { "contextType", _contextTypeName }
                    });
                }
                catch
                {
                    // HasPendingModelChanges throws if there are pending changes
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

                var output = string.Join(Environment.NewLine, summary);
                _logger.LogInformation("Database migration status: {Status}", output);

                return new EFOperationResult { Success = true, Output = output };
            }
            catch (Exception ex)
            {
                var errorMessage = GetInnerExceptionMessage(ex);
                // Check if database doesn't exist
                if (errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                    errorMessage.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase))
                {
                    return new EFOperationResult 
                    { 
                        Success = true, 
                        Output = "Database has not been created yet.\nRun 'Update Database' to create and apply migrations." 
                    };
                }
                return new EFOperationResult { Success = false, ErrorMessage = errorMessage };
            }
        }, _cancellationToken);
    }

    public Task<EFOperationResult> GenerateMigrationScriptAsync(string? outputPath = null)
    {
        return Task.Run(() =>
        {
            var initResult = EnsureInitialized();
            if (!initResult.Success)
            {
                return initResult;
            }

            try
            {
                var script = InvokeOperation<string>("ScriptMigration", new Dictionary<string, object?>
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

                return new EFOperationResult { Success = true, Output = script };
            }
            catch (Exception ex)
            {
                return new EFOperationResult { Success = false, ErrorMessage = GetInnerExceptionMessage(ex) };
            }
        }, _cancellationToken);
    }

    public Task<EFOperationResult> GenerateMigrationBundleAsync(string? outputPath = null)
    {
        // Note: Migration bundles require using dotnet ef migrations bundle command
        // as there's no direct API for this in OperationExecutor
        // Include initialization check to match other methods
        var initResult = EnsureInitialized();
        if (!initResult.Success)
        {
            return Task.FromResult(initResult);
        }

        _ = outputPath; // Unused - included for API consistency
        _logger.LogWarning("Migration bundle generation is not yet implemented via reflection. Use 'dotnet ef migrations bundle' from the command line.");
        return Task.FromResult(new EFOperationResult 
        { 
            Success = false, 
            ErrorMessage = "Migration bundle generation requires the dotnet ef CLI. Use 'dotnet ef migrations bundle' from the command line." 
        });
    }

    private void InvokeOperation(string operationName, IDictionary arguments)
    {
        InvokeOperationImpl(operationName, arguments);
    }

    private TResult InvokeOperation<TResult>(string operationName, IDictionary arguments)
    {
        return (TResult)InvokeOperationImpl(operationName, arguments);
    }

    private object InvokeOperationImpl(string operationName, IDictionary arguments)
    {
        if (_executor == null || _commandsAssembly == null || _resultHandlerType == null)
        {
            throw new InvalidOperationException("Executor not initialized.");
        }

        var resultHandler = Activator.CreateInstance(_resultHandlerType)!;

        // Execute the operation by creating the nested operation type
        var operationType = _commandsAssembly.GetType($"{ExecutorTypeName}+{operationName}", throwOnError: true, ignoreCase: true)!;
        Activator.CreateInstance(operationType, _executor, resultHandler, arguments);

        // Check for errors
        var errorType = (string?)_resultHandlerType.GetProperty("ErrorType")?.GetValue(resultHandler);
        if (errorType != null)
        {
            var errorMessage = (string?)_resultHandlerType.GetProperty("ErrorMessage")?.GetValue(resultHandler);
            throw new InvalidOperationException(errorMessage ?? "Unknown error occurred.");
        }

        return _resultHandlerType.GetProperty("Result")?.GetValue(resultHandler)!;
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

    private string? GetProjectPath()
    {
        if (_projectResource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            return metadata.ProjectPath;
        }
        return null;
    }

    private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        if (_appBasePath == null)
        {
            return null;
        }

        var assemblyName = new AssemblyName(args.Name);

        foreach (var extension in new[] { ".dll", ".exe" })
        {
            var path = Path.Combine(_appBasePath, assemblyName.Name + extension);
            if (File.Exists(path))
            {
                try
                {
#if NET
                    return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
#else
                    return Assembly.LoadFrom(path);
#endif
                }
                catch
                {
                    // Ignore loading errors
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
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
