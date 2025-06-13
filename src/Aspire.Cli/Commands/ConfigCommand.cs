// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfiguration configuration, IConfigurationService configurationService, IInteractionService interactionService)
        : base("config", "Manage configuration settings.")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configuration = configuration;
        _configurationService = configurationService;
        _interactionService = interactionService;

        var verbArgument = new Argument<string>("verb")
        {
            Description = "The operation to perform: get, set, list, or delete."
        };
        Arguments.Add(verbArgument);

        var remainingArguments = new Argument<string[]>("args")
        {
            Description = "Additional arguments for the operation.",
            Arity = ArgumentArity.ZeroOrMore
        };
        Arguments.Add(remainingArguments);

        var globalOption = new Option<bool>("--global", "-g")
        {
            Description = "Use global configuration in $HOME/.aspire/settings.json instead of the local settings file."
        };
        Options.Add(globalOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var verb = parseResult.GetValue<string>("verb");
        if (string.IsNullOrEmpty(verb))
        {
            _interactionService.DisplayError("Operation verb is required. Use: get, set, list, or delete.");
            return 1;
        }

        var args = parseResult.GetValue<string[]>("args") ?? [];

        return verb.ToLowerInvariant() switch
        {
            "get" => await ExecuteGetAsync(args, parseResult, cancellationToken),
            "set" => await ExecuteSetAsync(args, parseResult, cancellationToken),
            "list" => await ExecuteListAsync(args, parseResult, cancellationToken),
            "delete" => await ExecuteDeleteAsync(args, parseResult, cancellationToken),
            _ => InvalidVerb(verb)
        };
    }

    private int InvalidVerb(string verb)
    {
        _interactionService.DisplayError($"Unknown operation '{verb}'. Valid operations are: get, set, list, delete.");
        return 1;
    }

    private Task<int> ExecuteGetAsync(string[] args, ParseResult _, CancellationToken __)
    {
        if (args.Length == 0)
        {
            _interactionService.DisplayError("Configuration key is required for get operation.");
            return Task.FromResult(1);
        }

        var key = args[0];
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

    private async Task<int> ExecuteSetAsync(string[] args, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
        {
            _interactionService.DisplayError("Configuration key and value are required for set operation.");
            return 1;
        }

        var key = args[0];
        var value = args[1];
        var isGlobal = parseResult.GetValue<bool>("--global");

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

    private async Task<int> ExecuteListAsync(string[] _, ParseResult __, CancellationToken cancellationToken)
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

    private async Task<int> ExecuteDeleteAsync(string[] args, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            _interactionService.DisplayError("Configuration key is required for delete operation.");
            return 1;
        }

        var key = args[0];
        var isGlobal = parseResult.GetValue<bool>("--global");

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