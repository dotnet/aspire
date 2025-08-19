// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class UpdateCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IInteractionService _interactionService;
    private readonly IPackagingService _packagingService;
    private readonly IDotNetCliRunner _runner;

    public UpdateCommand(IProjectLocator projectLocator, IPackagingService packagingService, IInteractionService interactionService, IDotNetCliRunner runner, IFeatures features, ICliUpdateNotifier updateNotifier) : base("update", UpdateCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(runner);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _packagingService = packagingService;
        _runner = runner;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // TODO: Decide on whether we want to support a non-interactive model here.
        var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(null, cancellationToken);
        var channels = await _packagingService.GetChannelsAsync(cancellationToken);

        var channel = await _interactionService.PromptForSelectionAsync("Select a channel:", channels, (c) => c.Name, cancellationToken);

        var collector = new OutputCollector();
        var options = new DotNetCliRunnerInvocationOptions();
        options.StandardOutputCallback += collector.AppendOutput;
        options.StandardErrorCallback += collector.AppendError;

        var (exitCode, document) = await _runner.GetProjectItemsAndPropertiesAsync(projectFile!, ["PackageReference", "ProjectReference"], ["AspireHostingSDKVersion"], new(), cancellationToken);

        if (exitCode != 0)
        {
            _interactionService.DisplayError("Failed to retrieve project items and properties.");
            _interactionService.DisplayLines(collector.GetLines());
            return ExitCodeConstants.FailedToUpgradeProject;
        }

        Console.WriteLine(document?.RootElement.ToString());

        return 0;
    }
}