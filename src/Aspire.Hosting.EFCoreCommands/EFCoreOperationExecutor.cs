// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aspire.Hosting;

/// <summary>
/// Executes EF Core design-time operations against a project.
/// </summary>
/// <remarks>
/// This class uses the dotnet ef CLI to execute design-time operations. The target project must reference
/// Microsoft.EntityFrameworkCore.Design package for these operations to work.
/// See: https://github.com/dotnet/efcore/blob/main/src/ef/ReflectionOperationExecutor.cs for the underlying implementation.
/// </remarks>
internal sealed class EFCoreOperationExecutor
{
    private readonly ProjectResource _projectResource;
    private readonly string? _contextTypeName;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

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

    public async Task<EFOperationResult> UpdateDatabaseAsync()
    {
        var args = BuildEFArgs("database", "update");
        return await ExecuteDotnetEFAsync(args).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> DropDatabaseAsync()
    {
        var args = BuildEFArgs("database", "drop", "--force");
        return await ExecuteDotnetEFAsync(args).ConfigureAwait(false);
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
        // If no migration name provided, use a default based on timestamp
        migrationName ??= $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogInformation("Creating migration with name: {MigrationName}", migrationName);

        var args = BuildEFArgs("migrations", "add", migrationName);
        return ExecuteDotnetEFAsync(args);
    }

    public async Task<EFOperationResult> RemoveMigrationAsync()
    {
        var args = BuildEFArgs("migrations", "remove", "--force");
        return await ExecuteDotnetEFAsync(args).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GetDatabaseStatusAsync()
    {
        // Get list of migrations and their applied status
        var args = BuildEFArgs("migrations", "list");
        var result = await ExecuteDotnetEFAsync(args).ConfigureAwait(false);

        if (result.Success)
        {
            _logger.LogInformation("Database migration status retrieved successfully.");
        }

        return result;
    }

    public async Task<EFOperationResult> GenerateMigrationScriptAsync(string? outputPath = null)
    {
        var args = new List<string> { "migrations", "script", "--idempotent" };

        if (outputPath != null)
        {
            args.Add("--output");
            args.Add(outputPath);
        }

        return await ExecuteDotnetEFAsync(BuildEFArgs([.. args])).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GenerateMigrationBundleAsync(string? outputPath = null)
    {
        var args = new List<string> { "migrations", "bundle", "--force" };

        if (outputPath != null)
        {
            args.Add("--output");
            args.Add(outputPath);
        }

        return await ExecuteDotnetEFAsync(BuildEFArgs([.. args])).ConfigureAwait(false);
    }

    private string[] BuildEFArgs(params string[] command)
    {
        var args = new List<string> { "ef" };
        args.AddRange(command);

        if (!string.IsNullOrEmpty(_contextTypeName))
        {
            args.Add("--context");
            args.Add(_contextTypeName);
        }

        // Add project path
        var projectPath = GetProjectPath();
        if (projectPath != null)
        {
            args.Add("--project");
            args.Add(projectPath);
        }

        return [.. args];
    }

    private string? GetProjectPath()
    {
        if (_projectResource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            return metadata.ProjectPath;
        }
        return null;
    }

    private async Task<EFOperationResult> ExecuteDotnetEFAsync(string[] args)
    {
        var projectPath = GetProjectPath();

        if (projectPath == null)
        {
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = "Could not determine project path."
            };
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);

        _logger.LogInformation("Executing: dotnet {Args}", string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(startInfo);

            if (process == null)
            {
                return new EFOperationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start dotnet process."
                };
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(_cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(_cancellationToken);

            await process.WaitForExitAsync(_cancellationToken).ConfigureAwait(false);

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogInformation("[EF] {Output}", line.TrimEnd());
                }
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogWarning("[EF] {Error}", line.TrimEnd());
                }
            }

            if (process.ExitCode == 0)
            {
                return new EFOperationResult { Success = true, Output = output };
            }
            else
            {
                return new EFOperationResult
                {
                    Success = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(error) ? output : error,
                    Output = output
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing dotnet ef command.");
            return new EFOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
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
