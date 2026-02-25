// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Shared.UserSecrets;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Secrets;

/// <summary>
/// Resolves the UserSecretsId for an AppHost and creates a SecretsStore.
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
                return null;
            }

            // Only .NET projects support dotnet user-secrets init
            if (!project.LanguageId.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

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
        interactionService.DisplayMessage("key", $"Initializing user secrets for {appHostFile.Name.EscapeMarkup()}...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "user-secrets", "init", "--project", appHostFile.FullName },
            WorkingDirectory = appHostFile.Directory!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start 'dotnet user-secrets init'.");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to initialize user secrets for '{appHostFile.Name.EscapeMarkup()}'. Exit code: {process.ExitCode}");
        }

        // Re-query to get the newly created UserSecretsId
        var userSecretsId = await project.GetUserSecretsIdAsync(appHostFile, cancellationToken);

        if (string.IsNullOrEmpty(userSecretsId))
        {
            throw new InvalidOperationException(
                $"User secrets were initialized but UserSecretsId could not be read from '{appHostFile.Name.EscapeMarkup()}'.");
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
