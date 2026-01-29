// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

internal sealed class CacheCommand : BaseCommand
{
    private readonly IConfiguration _configuration;

    public CacheCommand(IConfiguration configuration, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
        : base("cache", CacheCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configuration = configuration;

        var clearCommand = new ClearCommand(configuration, InteractionService, features, updateNotifier, executionContext, telemetry);

        Subcommands.Add(clearCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is not "true")
        {
            new HelpAction().Invoke(parseResult);
            return ExitCodeConstants.InvalidCommand;
        }

        // Prompt for the action that the user wants to perform
        var subcommand = await InteractionService.PromptForSelectionAsync(
            CacheCommandStrings.ExtensionActionPrompt,
            Subcommands.Cast<ClearCommand>(),
            cmd =>
            {
                Debug.Assert(cmd.Description is not null);
                return cmd.Description.TrimEnd('.');
            },
            cancellationToken);

        return await subcommand.InteractiveExecuteAsync(cancellationToken);
    }

    private sealed class ClearCommand : BaseCommand
    {
        private readonly IConfiguration _configuration;

        public ClearCommand(IConfiguration configuration, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, AspireCliTelemetry telemetry)
            : base("clear", CacheCommandStrings.ClearCommand_Description, features, updateNotifier, executionContext, interactionService, telemetry)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _configuration = configuration;
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is "true")
            {
                return InteractiveExecuteAsync(cancellationToken);
            }

            return ExecuteClearAsync(cancellationToken);
        }

        public async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
        {
            // Prompt for confirmation
            var confirmed = await InteractionService.PromptForSelectionAsync(
                CacheCommandStrings.ClearCommand_ConfirmationPrompt,
                [true, false],
                choice => choice ? TemplatingStrings.Yes : TemplatingStrings.No,
                cancellationToken);

            if (!confirmed)
            {
                InteractionService.DisplayCancellationMessage();
                return ExitCodeConstants.Success;
            }

            return await ExecuteClearAsync(cancellationToken);
        }

        private Task<int> ExecuteClearAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cacheDirectory = ExecutionContext.CacheDirectory;
                var filesDeleted = 0;
                
                // Delete cache files and subdirectories
                if (cacheDirectory.Exists)
                {
                    // Delete all cache files and subdirectories
                    foreach (var file in cacheDirectory.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            file.Delete();
                            filesDeleted++;
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                        {
                            // Continue deleting other files even if some fail
                        }
                    }

                    // Delete subdirectories
                    foreach (var directory in cacheDirectory.GetDirectories())
                    {
                        try
                        {
                            directory.Delete(recursive: true);
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                        {
                            // Continue deleting other directories even if some fail
                        }
                    }
                }

                // Also clear the sdks directory
                var sdksDirectory = ExecutionContext.SdksDirectory;
                if (sdksDirectory.Exists)
                {
                    foreach (var file in sdksDirectory.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            file.Delete();
                            filesDeleted++;
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                        {
                            // Continue deleting other files even if some fail
                        }
                    }

                    // Delete subdirectories
                    foreach (var directory in sdksDirectory.GetDirectories())
                    {
                        try
                        {
                            directory.Delete(recursive: true);
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                        {
                            // Continue deleting other directories even if some fail
                        }
                    }
                }

                if (filesDeleted == 0)
                {
                    InteractionService.DisplayMessage("information", CacheCommandStrings.CacheAlreadyEmpty);
                }
                else
                {
                    InteractionService.DisplaySuccess(CacheCommandStrings.CacheCleared);
                }

                return Task.FromResult(ExitCodeConstants.Success);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(CultureInfo.CurrentCulture, CacheCommandStrings.CacheClearFailed, ex.Message);
                Telemetry.RecordError(errorMessage, ex);
                InteractionService.DisplayError(errorMessage);
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }
        }
    }
}