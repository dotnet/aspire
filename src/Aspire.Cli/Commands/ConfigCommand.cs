// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfigurationService configurationService, IInteractionService interactionService)
        : base("config", "Manage configuration settings.")
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configurationService = configurationService;
        _interactionService = interactionService;

        var getCommand = new GetCommand(_configurationService, _interactionService);
        var setCommand = new SetCommand(_configurationService, _interactionService);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // When no subcommand is provided, the system will automatically show help and return 0
        return Task.FromResult(0);
    }

    private sealed class GetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IInteractionService _interactionService;

        public GetCommand(IConfigurationService configurationService, IInteractionService interactionService)
            : base("get", "Get a configuration value.")
        {
            _configurationService = configurationService;
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

            var value = _configurationService.GetConfiguration(key);

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
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var value = parseResult.GetValue<string>("value");

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
                await _configurationService.SetConfigurationAsync(key, value, cancellationToken);
                _interactionService.DisplaySuccess($"Configuration '{key}' set to '{value}'.");
                return 0;
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError($"Error setting configuration: {ex.Message}");
                return 1;
            }
        }
    }
}