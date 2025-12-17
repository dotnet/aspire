// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Diagnostics;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected virtual bool UpdateNotificationsEnabled { get; } = true;
    private readonly CliExecutionContext _executionContext;
    private readonly ILogger? _logger;
    private readonly FileLoggerProvider? _fileLoggerProvider;

    protected CliExecutionContext ExecutionContext => _executionContext;

    protected IInteractionService InteractionService { get; }

    protected BaseCommand(string name, string description, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, IInteractionService interactionService, ILoggerFactory? loggerFactory = null, FileLoggerProvider? fileLoggerProvider = null) : base(name, description)
    {
        _executionContext = executionContext;
        InteractionService = interactionService;
        _logger = loggerFactory?.CreateLogger(GetType());
        _fileLoggerProvider = fileLoggerProvider;
        SetAction(async (parseResult, cancellationToken) =>
        {
            // Set the command on the execution context so background services can access it
            _executionContext.Command = this;

            // TODO: SDK install goes here in the future.

            var exitCode = await ExecuteAsync(parseResult, cancellationToken);

            if (UpdateNotificationsEnabled && features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
            {
                try
                {
                    updateNotifier.NotifyIfUpdateAvailable();
                }
                catch
                {
                    // Ignore any errors during update check to avoid impacting the main command
                }
            }

            InteractionService.DisplayEmptyLine();

            return exitCode;
        });
    }

    protected abstract Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);

    /// <summary>
    /// Logs an exception and displays it to the user.
    /// </summary>
    protected Task<int> HandleExceptionAsync(Exception ex, int exitCode, string? additionalContext = null)
    {
        // Log the error (file logger will capture this and write diagnostics bundle)
        if (_logger != null)
        {
            var message = additionalContext ?? ex.Message;
            _logger.LogError(ex, "Command '{CommandName}' failed: {Message}", this.Name, message);
        }

        // Get the diagnostics path from the file logger provider
        var bundlePath = _fileLoggerProvider?.GetDiagnosticsPath();

        // Display error with appropriate detail
        ErrorDisplayHelper.DisplayException(
            InteractionService,
            ex,
            exitCode,
            ExecutionContext,
            bundlePath);

        return Task.FromResult(exitCode);
    }

    internal static int HandleProjectLocatorException(ProjectLocatorException ex, IInteractionService interactionService)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(interactionService);

        if (string.Equals(ex.Message, ErrorStrings.ProjectFileNotAppHostProject, StringComparisons.CliInputOrOutput))
        {
            interactionService.DisplayError(InteractionServiceStrings.SpecifiedProjectFileNotAppHostProject);
            return ExitCodeConstants.FailedToFindProject;
        }
        if (string.Equals(ex.Message, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput))
        {
            interactionService.DisplayError(InteractionServiceStrings.ProjectOptionDoesntExist);
            return ExitCodeConstants.FailedToFindProject;
        }
        if (string.Equals(ex.Message, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput))
        {
            interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        if (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
        {
            interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        if (string.Equals(ex.Message, ErrorStrings.AppHostsMayNotBeBuildable, StringComparisons.CliInputOrOutput))
        {
            interactionService.DisplayError(InteractionServiceStrings.UnbuildableAppHostsDetected);
            return ExitCodeConstants.FailedToFindProject;
        }

        interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
        return ExitCodeConstants.FailedToFindProject;
    }
}
