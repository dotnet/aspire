// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfigurationService _configurationService;
    private readonly IAnsiConsole _ansiConsole;

    public ConfigCommand(IConfigurationService configurationService, IAnsiConsole ansiConsole)
        : base("config", "Manage configuration settings.")
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(ansiConsole);

        _configurationService = configurationService;
        _ansiConsole = ansiConsole;

        var getCommand = new GetCommand(_configurationService, _ansiConsole);
        var setCommand = new SetCommand(_configurationService, _ansiConsole);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // If no subcommand is provided, show help
        _ansiConsole.WriteLine("Use 'aspire config --help' to see available commands.");
        return Task.FromResult(1);
    }

    private sealed class GetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IAnsiConsole _ansiConsole;

        public GetCommand(IConfigurationService configurationService, IAnsiConsole ansiConsole)
            : base("get", "Get a configuration value.")
        {
            _configurationService = configurationService;
            _ansiConsole = ansiConsole;

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
                _ansiConsole.WriteLine("Configuration key is required.");
                return Task.FromResult(1);
            }

            var value = _configurationService.GetConfiguration(key);

            if (value is not null)
            {
                _ansiConsole.WriteLine(value);
                return Task.FromResult(0);
            }
            else
            {
                _ansiConsole.WriteLine($"Configuration key '{key}' not found.");
                return Task.FromResult(1);
            }
        }
    }

    private sealed class SetCommand : BaseCommand
    {
        private readonly IConfigurationService _configurationService;
        private readonly IAnsiConsole _ansiConsole;

        public SetCommand(IConfigurationService configurationService, IAnsiConsole ansiConsole)
            : base("set", "Set a configuration value.")
        {
            _configurationService = configurationService;
            _ansiConsole = ansiConsole;

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
                _ansiConsole.WriteLine("Configuration key is required.");
                return 1;
            }

            if (value is null)
            {
                _ansiConsole.WriteLine("Configuration value is required.");
                return 1;
            }

            try
            {
                await _configurationService.SetConfigurationAsync(key, value, cancellationToken);
                _ansiConsole.WriteLine($"Configuration '{key}' set to '{value}'.");
                return 0;
            }
            catch (Exception ex)
            {
                _ansiConsole.WriteLine($"Error setting configuration: {ex.Message}");
                return 1;
            }
        }
    }
}