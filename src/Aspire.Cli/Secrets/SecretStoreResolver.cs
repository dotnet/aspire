// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
    IAppHostProjectFactory projectFactory,
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

        var project = projectFactory.TryGetProject(appHostFile);
        if (project is null)
        {
            return null;
        }

        // Delegate UserSecretsId resolution to the project handler
        var userSecretsId = await project.GetUserSecretsIdAsync(appHostFile, cancellationToken);

        if (string.IsNullOrEmpty(userSecretsId))
        {
            if (!autoInit)
            {
                throw new InvalidOperationException(
                    $"No UserSecretsId configured for '{appHostFile.Name}'. Run 'dotnet user-secrets init' in the AppHost directory, or use 'aspire secret set' which will initialize it automatically.");
            }

            // Auto-initialize (only works for .NET projects)
            userSecretsId = await AutoInitUserSecretsAsync(appHostFile, project, cancellationToken);
        }

        var secretsFilePath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var store = new SecretsStore(secretsFilePath);

        return new SecretsStoreResult(store, userSecretsId, appHostFile);
    }

    private async Task<string> AutoInitUserSecretsAsync(
        FileInfo appHostFile,
        IAppHostProject project,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("No UserSecretsId found. Initializing user secrets for {Project}...", appHostFile.Name);
        interactionService.DisplayMessage("key", $"Initializing user secrets for {appHostFile.Name}...");

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "user-secrets", "init", "--project", appHostFile.FullName },
            WorkingDirectory = appHostFile.Directory!.FullName,
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
                $"Failed to initialize user secrets for '{appHostFile.Name}'. Exit code: {process.ExitCode}");
        }

        // Re-query to get the newly created UserSecretsId
        var userSecretsId = await project.GetUserSecretsIdAsync(appHostFile, cancellationToken);

        if (string.IsNullOrEmpty(userSecretsId))
        {
            throw new InvalidOperationException(
                $"User secrets were initialized but UserSecretsId could not be read from '{appHostFile.Name}'.");
        }

        logger.LogInformation("User secrets initialized. UserSecretsId: {UserSecretsId}", userSecretsId);
        return userSecretsId;
    }
}

/// <summary>
/// Result of resolving a secrets store for an AppHost.
/// </summary>
internal sealed record SecretsStoreResult(
    SecretsStore Store,
    string UserSecretsId,
    FileInfo AppHostFile);
