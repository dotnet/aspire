// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class ExtensionInternalCommand : BaseCommand
{
    public ExtensionInternalCommand(IFeatures features, ICliUpdateNotifier updateNotifier, IProjectLocator projectLocator, IDotNetCliRunner dotNetCliRunner, CliExecutionContext executionContext, IInteractionService interactionService) : base("extension", "Hidden command for extension integration", features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(updateNotifier);

        this.Hidden = true;
        this.Subcommands.Add(new GetAppHostCandidatesCommand(features, updateNotifier, projectLocator, executionContext, interactionService));
        this.Subcommands.Add(new BuildCommand(features, updateNotifier, dotNetCliRunner, executionContext, interactionService));
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExitCodeConstants.Success);
    }

    private sealed class GetAppHostCandidatesCommand : BaseCommand
    {
        private readonly IProjectLocator _projectLocator;

        public GetAppHostCandidatesCommand(IFeatures features, ICliUpdateNotifier updateNotifier, IProjectLocator projectLocator, CliExecutionContext executionContext, IInteractionService interactionService) : base("get-apphosts", "Get AppHosts in the specified directory", features, updateNotifier, executionContext, interactionService)
        {
            _projectLocator = projectLocator;

            var directoryOption = new Option<string?>("--directory");
            directoryOption.Description = "The directory to search for AppHost projects. Defaults to the current directory.";
            Options.Add(directoryOption);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _projectLocator.UseOrFindAppHostProjectFileAsync(null, MultipleAppHostProjectsFoundBehavior.None, createSettingsFile: false, cancellationToken);

                var json = JsonSerializer.Serialize(new AppHostProjectSearchResultPoco
                {
                    SelectedProjectFile = result.SelectedProjectFile?.FullName,
                    AllProjectFileCandidates = result.AllProjectFileCandidates.Select(f => f.FullName).ToList()
                }, BackchannelJsonSerializerContext.Default.AppHostProjectSearchResultPoco);
                InteractionService.DisplayRawText(json);
                return ExitCodeConstants.Success;
            }
            catch
            {
                return ExitCodeConstants.FailedToFindProject;
            }
        }
    }

    private sealed class BuildCommand : BaseCommand
    {
        private readonly IDotNetCliRunner _dotNetCliRunner;

        public BuildCommand(IFeatures features, ICliUpdateNotifier updateNotifier, IDotNetCliRunner dotNetCliRunner, CliExecutionContext executionContext, IInteractionService interactionService) : base("build", "Build a project file", features, updateNotifier, executionContext, interactionService)
        {
            _dotNetCliRunner = dotNetCliRunner;

            var projectOption = new Option<string?>("--project");
            projectOption.Description = "The project file to build.";
            projectOption.IsRequired = true;
            Options.Add(projectOption);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var projectPath = parseResult.GetValue<string?>("--project");

            if (string.IsNullOrEmpty(projectPath))
            {
                InteractionService.DisplayError("Project path is required.");
                return ExitCodeConstants.InvalidInput;
            }

            var projectFile = new FileInfo(projectPath);
            if (!projectFile.Exists)
            {
                InteractionService.DisplayError($"Project file not found: {projectPath}");
                return ExitCodeConstants.FailedToFindProject;
            }

            // Create options to stream output through the interaction service
            var options = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = (message) =>
                {
                    if (InteractionService is IExtensionInteractionService extensionInteractionService)
                    {
                        extensionInteractionService.LogMessage(LogLevel.Information, message);
                    }
                },
                StandardErrorCallback = (message) =>
                {
                    if (InteractionService is IExtensionInteractionService extensionInteractionService)
                    {
                        extensionInteractionService.LogMessage(LogLevel.Error, message);
                    }
                }
            };

            var exitCode = await _dotNetCliRunner.BuildAsync(projectFile, options, cancellationToken);
            return exitCode;
        }
    }
}

internal class AppHostProjectSearchResultPoco
{
    [JsonPropertyName("selected_project_file")]
    public string? SelectedProjectFile { get; init; }

    [JsonPropertyName("all_project_file_candidates")]
    public required List<string> AllProjectFileCandidates { get; init; }
}
