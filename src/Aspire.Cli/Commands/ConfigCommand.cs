// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfiguration configuration, IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotififier updateNotifier)
        : base("config", ConfigCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configuration = configuration;
        _configurationService = configurationService;
        _interactionService = interactionService;

        var getCommand = new GetCommand(configurationService, _interactionService, features, updateNotifier);
        var setCommand = new SetCommand(configurationService, _interactionService, features, updateNotifier);
        var listCommand = new ListCommand(configurationService, _interactionService, features, updateNotifier);
        var deleteCommand = new DeleteCommand(configurationService, _interactionService, features, updateNotifier);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(deleteCommand);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private sealed class GetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public GetCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotififier updateNotifier)
            : base("get", ConfigCommandStrings.GetCommand_Description, features, updateNotifier)
        {
            _configurationService = configurationService;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = ConfigCommandStrings.GetCommand_KeyArgumentDescription
            };
            Arguments.Add(keyArgument);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            if (key is null)
            {
                _interactionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return ExitCodeConstants.InvalidCommand;
            }

            var value = await _configurationService.GetConfigurationAsync(key, cancellationToken);

            if (value is not null)
            {
                _interactionService.DisplayPlainText(value);
                return ExitCodeConstants.Success;
            }
            else
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationKeyNotFound, key));
                return ExitCodeConstants.ConfigNotFound;
            }
        }
    }

    private sealed class SetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public SetCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotififier updateNotifier)
            : base("set", ConfigCommandStrings.SetCommand_Description, features, updateNotifier)
        {
            _configurationService = configurationService;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = ConfigCommandStrings.SetCommand_KeyArgumentDescription
            };
            Arguments.Add(keyArgument);

            var valueArgument = new Argument<string>("value")
            {
                Description = ConfigCommandStrings.SetCommand_ValueArgumentDescription
            };
            Arguments.Add(valueArgument);

            var globalOption = new Option<bool>("--global", "-g")
            {
                Description = ConfigCommandStrings.SetCommand_GlobalArgumentDescription
            };
            Options.Add(globalOption);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var value = parseResult.GetValue<string>("value");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                _interactionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return ExitCodeConstants.InvalidCommand;
            }

            if (value is null)
            {
                _interactionService.DisplayError(ErrorStrings.ConfigurationValueRequired);
                return ExitCodeConstants.InvalidCommand;
            }

            try
            {
                await _configurationService.SetConfigurationAsync(key, value, isGlobal, cancellationToken);
                _interactionService.DisplaySuccess(isGlobal
                    ? string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeySetGlobally, key,
                        value)
                    : string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeySetLocally, key,
                        value));

                return ExitCodeConstants.Success;
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ErrorSettingConfiguration, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }

    private sealed class ListCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public ListCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotififier updateNotifier)
            : base("list", ConfigCommandStrings.ListCommand_Description, features, updateNotifier)
        {
            _configurationService = configurationService;
            _interactionService = interactionService;
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var allConfig = await _configurationService.GetAllConfigurationAsync(cancellationToken);

            if (allConfig.Count == 0)
            {
                _interactionService.DisplayMessage("information", ConfigCommandStrings.NoConfigurationValuesFound);
                return ExitCodeConstants.Success;
            }

            foreach (var kvp in allConfig)
            {
                Console.WriteLine($"{kvp.Key}={kvp.Value}");
            }

            return ExitCodeConstants.Success;
        }
    }

    private sealed class DeleteCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public DeleteCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotififier updateNotifier)
            : base("delete", ConfigCommandStrings.DeleteCommand_Description, features, updateNotifier)
        {
            _configurationService = configurationService;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = ConfigCommandStrings.DeleteCommand_KeyArgumentDescription
            };
            Arguments.Add(keyArgument);

            var globalOption = new Option<bool>("--global", "-g")
            {
                Description = ConfigCommandStrings.DeleteCommand_GlobalArgumentDescription
            };
            Options.Add(globalOption);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                _interactionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return ExitCodeConstants.InvalidCommand;
            }

            try
            {
                var deleted = await _configurationService.DeleteConfigurationAsync(key, isGlobal, cancellationToken);

                if (deleted)
                {
                    var scope = isGlobal ? "globally" : "locally";
                    if (isGlobal)
                    {
                        _interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeyDeletedGlobally, key));
                    }
                    else
                    {
                        _interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeyDeletedLocally, key));
                    }

                    return ExitCodeConstants.Success;
                }
                else
                {
                    _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationKeyNotFound, key));
                    return ExitCodeConstants.InvalidCommand;
                }
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ErrorDeletingConfiguration, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }
}
