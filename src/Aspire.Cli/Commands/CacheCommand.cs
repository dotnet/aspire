// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class CacheCommand : BaseCommand
{
    public CacheCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : base("cache", CacheCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);

        var clearCommand = new ClearCommand(InteractionService, features, updateNotifier, executionContext);

        Subcommands.Add(clearCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private sealed class ClearCommand : BaseCommand
    {
        public ClearCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("clear", CacheCommandStrings.ClearCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            try
            {
                var cacheDirectory = ExecutionContext.CacheDirectory;
                
                if (!cacheDirectory.Exists)
                {
                    InteractionService.DisplayMessage("information", CacheCommandStrings.CacheAlreadyEmpty);
                    return Task.FromResult(ExitCodeConstants.Success);
                }

                var filesDeleted = 0;
                
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
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, CacheCommandStrings.CacheClearFailed, ex.Message));
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }
        }
    }
}