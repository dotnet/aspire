// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Shared.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Secrets;

/// <summary>
/// Resolves the UserSecretsId for an AppHost and creates a SecretsStore for it.
/// </summary>
internal sealed class SecretStoreResolver(
    IProjectLocator projectLocator,
    IDotNetCliRunner dotNetCliRunner,
    IInteractionService interactionService,
    ILogger<SecretStoreResolver> logger)
{
    /// <summary>
    /// Resolves the secrets store for the given (or auto-discovered) AppHost project.
    /// </summary>
    /// <param name="projectFile">Explicit project file, or null to auto-discover.</param>
    /// <param name="autoInit">If true, initializes UserSecretsId when missing (.NET projects only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A SecretsStore for the resolved AppHost, or null if the project could not be found.</returns>
    public async Task<SecretsStoreResult?> ResolveAsync(
        FileInfo? projectFile,
        bool autoInit,
        CancellationToken cancellationToken)
    {
        // Discover the AppHost project
        var searchResult = await projectLocator.UseOrFindAppHostProjectFileAsync(
            projectFile,
            MultipleAppHostProjectsFoundBehavior.Prompt,
            createSettingsFile: false,
            cancellationToken);

        var appHostFile = searchResult.SelectedProjectFile;
        if (appHostFile is null)
        {
            return null;
        }

        // Determine if this is a .NET project (csproj/fsproj/vbproj) or polyglot
        var extension = appHostFile.Extension.ToLowerInvariant();
        var isDotNetProject = extension is ".csproj" or ".fsproj" or ".vbproj";

        string userSecretsId;

        if (isDotNetProject)
        {
            userSecretsId = await ResolveDotNetUserSecretsIdAsync(appHostFile, autoInit, cancellationToken);
        }
        else
        {
            // Polyglot: compute synthetic UserSecretsId from the absolute path
            userSecretsId = ComputeSyntheticUserSecretsId(appHostFile.FullName);
            logger.LogDebug("Using synthetic UserSecretsId for polyglot AppHost: {UserSecretsId}", userSecretsId);
        }

        var secretsFilePath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var store = new SecretsStore(secretsFilePath);

        return new SecretsStoreResult(store, userSecretsId, appHostFile);
    }

    private async Task<string> ResolveDotNetUserSecretsIdAsync(
        FileInfo projectFile,
        bool autoInit,
        CancellationToken cancellationToken)
    {
        // Query MSBuild for UserSecretsId
        var userSecretsId = await GetUserSecretsIdFromProjectAsync(projectFile, cancellationToken);

        if (!string.IsNullOrEmpty(userSecretsId))
        {
            logger.LogDebug("Found UserSecretsId from project: {UserSecretsId}", userSecretsId);
            return userSecretsId;
        }

        if (!autoInit)
        {
            throw new InvalidOperationException(
                $"No UserSecretsId configured for '{projectFile.Name}'. Run 'dotnet user-secrets init' in the AppHost directory, or use 'aspire secret set' which will initialize it automatically.");
        }

        // Auto-initialize UserSecretsId
        logger.LogInformation("No UserSecretsId found. Initializing user secrets for {Project}...", projectFile.Name);
        interactionService.DisplayMessage("key", $"Initializing user secrets for {projectFile.Name}...");

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "user-secrets", "init", "--project", projectFile.FullName },
            WorkingDirectory = projectFile.Directory!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start 'dotnet user-secrets init'.");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to initialize user secrets for '{projectFile.Name}'. Exit code: {process.ExitCode}");
        }

        // Re-query to get the newly created UserSecretsId
        userSecretsId = await GetUserSecretsIdFromProjectAsync(projectFile, cancellationToken);

        if (string.IsNullOrEmpty(userSecretsId))
        {
            throw new InvalidOperationException(
                $"User secrets were initialized but UserSecretsId could not be read from '{projectFile.Name}'.");
        }

        logger.LogInformation("User secrets initialized. UserSecretsId: {UserSecretsId}", userSecretsId);
        return userSecretsId;
    }

    private async Task<string?> GetUserSecretsIdFromProjectAsync(
        FileInfo projectFile,
        CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, jsonDocument) = await dotNetCliRunner.GetProjectItemsAndPropertiesAsync(
                projectFile,
                items: [],
                properties: ["UserSecretsId"],
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken);

            if (exitCode != 0 || jsonDocument is null)
            {
                return null;
            }

            var rootElement = jsonDocument.RootElement;
            if (rootElement.TryGetProperty("Properties", out var properties) &&
                properties.TryGetProperty("UserSecretsId", out var userSecretsIdElement))
            {
                var value = userSecretsIdElement.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get UserSecretsId from project file.");
        }

        return null;
    }

    /// <summary>
    /// Computes a deterministic synthetic UserSecretsId from an AppHost file path.
    /// </summary>
    internal static string ComputeSyntheticUserSecretsId(string appHostPath)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(appHostPath.ToLowerInvariant()));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"aspire-{hash[..32]}";
    }
}

/// <summary>
/// Result of resolving a secrets store for an AppHost.
/// </summary>
internal sealed record SecretsStoreResult(
    SecretsStore Store,
    string UserSecretsId,
    FileInfo AppHostFile);
