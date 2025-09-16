// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class ExtensionInternalCommand : BaseCommand
{
    public ExtensionInternalCommand(IFeatures features, ICliUpdateNotifier updateNotifier, IProjectLocator projectLocator, CliExecutionContext executionContext, IInteractionService interactionService) : base("extension", "Hidden command for extension integration", features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(updateNotifier);

        this.Hidden = true;
        this.Subcommands.Add(new GetAppHostCandidatesCommand(features, updateNotifier, projectLocator, executionContext, interactionService));
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
            var files = await _projectLocator.FindAppHostProjectFilesAsync(parseResult.GetValue<string?>("--directory") ?? Environment.CurrentDirectory, cancellationToken);
            var jsonContent = JsonSerializer.Serialize(files.Select(f => f.FullName).ToList(), BackchannelJsonSerializerContext.Default.ListString);
            Console.WriteLine(jsonContent);
            return ExitCodeConstants.Success;
        }
    }
}
