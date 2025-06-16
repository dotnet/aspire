// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfiguration configuration, IConfigurationService configurationService, IInteractionService interactionService)
        : base("config", ConfigCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configuration = configuration;
        _configurationService = configurationService;
        _interactionService = interactionService;

        var getCommand = new GetCommand(_configuration, _interactionService);
        var setCommand = new SetCommand(configurationService, _interactionService);
        var listCommand = new ListCommand(configurationService, _interactionService);
        var deleteCommand = new DeleteCommand(configurationService, _interactionService);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(deleteCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private sealed class GetCommand : BaseCommand
    {
        private readonly IConfiguration _configuration;
        private readonly IInteractionService _interactionService;

        public GetCommand(IConfiguration configuration, IInteractionService interactionService)
            : base("get", "Get a configuration value.")
        {
            _configuration = configuration;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = "The configuration key to retrieve."
            };
            Arguments.Add(keyArgument);
        }

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            if (key is null)
            {
                _interactionService.DisplayError("Configuration key is required.");
                return Task.FromResult(1);
            }

            var value = _configuration[key];

            if (value is not null)
            {
                Console.WriteLine(value);
                return Task.FromResult(0);
            }
            else
            {
                _interactionService.DisplayError($"Configuration key '{key}' not found.");
                return Task.FromResult(1);
            }
        }
    }

    private sealed class SetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public SetCommand(IConfigurationService configurationService, IInteractionService interactionService)
            : base("set", "Set a configuration value.")
        {
            _configurationService = configurationService;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = "The configuration key to set."
            };
            Arguments.Add(keyArgument);

            var valueArgument = new Argument<string>("value")
            {
                Description = "The configuration value to set."
            };
            Arguments.Add(valueArgument);

            var globalOption = new Option<bool>("--global", "-g")
            {
                Description = "Set the configuration value globally in $HOME/.aspire/settings.json instead of the local settings file."
            };
            Options.Add(globalOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var value = parseResult.GetValue<string>("value");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                _interactionService.DisplayError("Configuration key is required.");
                return 1;
            }

            if (value is null)
            {
                _interactionService.DisplayError("Configuration value is required.");
                return 1;
            }

            try
            {
                await _configurationService.SetConfigurationAsync(key, value, isGlobal, cancellationToken);
                var scope = isGlobal ? "globally" : "locally";
                _interactionService.DisplaySuccess($"Configuration '{key}' set to '{value}' {scope}.");
                return 0;
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError($"Error setting configuration: {ex.Message}");
                return 1;
            }
        }
    }

    private sealed class ListCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public ListCommand(IConfigurationService configurationService, IInteractionService interactionService)
            : base("list", "List all configuration values.")
        {
            _configurationService = configurationService;
            _interactionService = interactionService;
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var allConfig = await _configurationService.GetAllConfigurationAsync(cancellationToken);

            if (allConfig.Count == 0)
            {
                _interactionService.DisplayMessage("ℹ️", "No configuration values found.");
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

        public DeleteCommand(IConfigurationService configurationService, IInteractionService interactionService)
            : base("delete", "Delete a configuration value.")
        {
            _configurationService = configurationService;
            _interactionService = interactionService;

            var keyArgument = new Argument<string>("key")
            {
                Description = "The configuration key to delete."
            };
            Arguments.Add(keyArgument);

            var globalOption = new Option<bool>("--global", "-g")
            {
                Description = "Delete the configuration value from the global $HOME/.aspire/settings.json instead of the local settings file."
            };
            Options.Add(globalOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                _interactionService.DisplayError("Configuration key is required.");
                return 1;
            }

            try
            {
                var deleted = await _configurationService.DeleteConfigurationAsync(key, isGlobal, cancellationToken);

                if (deleted)
                {
                    var scope = isGlobal ? "globally" : "locally";
                    _interactionService.DisplaySuccess($"Configuration '{key}' deleted {scope}.");
                    return 0;
                }
                else
                {
                    _interactionService.DisplayError($"Configuration key '{key}' not found.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError($"Error deleting configuration: {ex.Message}");
                return 1;
            }
        }
    }
}
