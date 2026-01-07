// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;

namespace Aspire.Cli.Projects;

/// <summary>
/// Service for managing AppHost language selection and persistence.
/// </summary>
internal sealed class LanguageService : ILanguageService
{
    private const string LanguageConfigKey = "language";

    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;
    private readonly IAppHostProjectFactory _projectFactory;

    public LanguageService(
        IConfigurationService configurationService,
        IInteractionService interactionService,
        IAppHostProjectFactory projectFactory)
    {
        _configurationService = configurationService;
        _interactionService = interactionService;
        _projectFactory = projectFactory;
    }

    /// <inheritdoc />
    public async Task<IAppHostProject?> GetConfiguredProjectAsync(CancellationToken cancellationToken = default)
    {
        var languageId = await _configurationService.GetConfigurationAsync(LanguageConfigKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(languageId))
        {
            return null;
        }

        return _projectFactory.GetProjectByLanguageId(languageId);
    }

    /// <inheritdoc />
    public async Task SetLanguageAsync(IAppHostProject project, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        await _configurationService.SetConfigurationAsync(
            LanguageConfigKey,
            project.LanguageId,
            isGlobal,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IAppHostProject> PromptForProjectAsync(CancellationToken cancellationToken = default)
    {
        var projects = _projectFactory.GetAllProjects().ToList();

        // If only one project is available, return it without prompting
        if (projects.Count == 1)
        {
            return projects[0];
        }

        var projectDict = projects.ToDictionary(p => p, p => p.DisplayName);

        _interactionService.DisplayEmptyLine();
        _interactionService.DisplayMarkdown("""
            # Select AppHost Language

            Choose the programming language for your Aspire AppHost.
            This selection will be saved for future use.
            """);
        _interactionService.DisplayEmptyLine();

        var selected = await _interactionService.PromptForSelectionAsync(
            "Which language would you like to use?",
            projectDict,
            kvp => kvp.Value,
            cancellationToken);

        return selected.Key;
    }

    /// <inheritdoc />
    public async Task<IAppHostProject> GetOrPromptForProjectAsync(
        string? explicitLanguageId = null,
        bool saveSelection = true,
        CancellationToken cancellationToken = default)
    {
        // If explicitly specified, use that
        if (!string.IsNullOrWhiteSpace(explicitLanguageId))
        {
            var project = _projectFactory.GetProjectByLanguageId(explicitLanguageId);
            if (project is not null)
            {
                return project;
            }

            var validLanguages = string.Join(", ", _projectFactory.GetAllProjects().Select(p => p.LanguageId));
            _interactionService.DisplayError($"Unknown language: '{explicitLanguageId}'. Valid options are: {validLanguages}");
            throw new ArgumentException($"Unknown language: '{explicitLanguageId}'", nameof(explicitLanguageId));
        }

        // Check if configured
        var configuredProject = await GetConfiguredProjectAsync(cancellationToken);
        if (configuredProject is not null)
        {
            return configuredProject;
        }

        // Prompt for selection
        var selectedProject = await PromptForProjectAsync(cancellationToken);

        // Save the selection
        if (saveSelection)
        {
            await SetLanguageAsync(selectedProject, isGlobal: false, cancellationToken);
            _interactionService.DisplayMessage("check_mark", $"Language preference saved to local settings: {selectedProject.DisplayName}");
        }

        return selectedProject;
    }
}
