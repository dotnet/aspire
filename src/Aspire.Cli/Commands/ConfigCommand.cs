// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationWriter _configurationWriter;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfiguration configuration, IConfigurationWriter configurationWriter, IInteractionService interactionService)
        : base("config", "Manage configuration settings.")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationWriter);
        ArgumentNullException.ThrowIfNull(interactionService);

        _configuration = configuration;
        _configurationWriter = configurationWriter;
        _interactionService = interactionService;

        var getCommand = new GetCommand(_configuration, _interactionService);
        var setCommand = new SetCommand(_configurationWriter, _interactionService);
        var listCommand = new ListCommand(_configuration, _interactionService);
        var deleteCommand = new DeleteCommand(_configurationWriter, _interactionService);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(deleteCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // When no subcommand is provided, the system will automatically show help and return 0
        return Task.FromResult(0);
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
        private readonly IConfigurationWriter _configurationWriter;
        private readonly IInteractionService _interactionService;

        public SetCommand(IConfigurationWriter configurationWriter, IInteractionService interactionService)
            : base("set", "Set a configuration value.")
        {
            _configurationWriter = configurationWriter;
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
                await _configurationWriter.SetConfigurationAsync(key, value, isGlobal, cancellationToken);
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
        private readonly IConfiguration _configuration;
        private readonly IInteractionService _interactionService;

        public ListCommand(IConfiguration configuration, IInteractionService interactionService)
            : base("list", "List all configuration values.")
        {
            _configuration = configuration;
            _interactionService = interactionService;
        }

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var allConfig = GetAllAspireConfiguration();

            if (allConfig.Count == 0)
            {
                _interactionService.DisplayMessage("ℹ️", "No configuration values found.");
                return Task.FromResult(0);
            }

            foreach (var kvp in allConfig)
            {
                Console.WriteLine($"{kvp.Key}={kvp.Value}");
            }

            return Task.FromResult(0);
        }

        private static SortedDictionary<string, string> GetAllAspireConfiguration()
        {
            var allConfig = new SortedDictionary<string, string>();
            
            // Get global settings first
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var globalSettingsPath = Path.Combine(homeDirectory, ".aspire", "settings.json");
            if (File.Exists(globalSettingsPath))
            {
                LoadConfigurationFromFile(globalSettingsPath, allConfig);
            }
            
            // Then walk up directory tree for local settings (they override global)
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            var settingsFiles = new List<string>();
            
            while (currentDirectory is not null)
            {
                var settingsFilePath = Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");
                if (File.Exists(settingsFilePath))
                {
                    settingsFiles.Add(settingsFilePath);
                }
                currentDirectory = currentDirectory.Parent;
            }
            
            // Process in reverse order so closer files take precedence
            settingsFiles.Reverse();
            foreach (var settingsFile in settingsFiles)
            {
                LoadConfigurationFromFile(settingsFile, allConfig);
            }
            
            return allConfig;
        }

        private static void LoadConfigurationFromFile(string filePath, SortedDictionary<string, string> config)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var settings = JsonNode.Parse(content)?.AsObject();
                
                if (settings is not null)
                {
                    foreach (var kvp in settings)
                    {
                        if (kvp.Value is not null)
                        {
                            config[kvp.Key] = kvp.Value.ToString();
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors reading configuration files
            }
        }
    }

    private sealed class DeleteCommand : BaseCommand
    {
        private readonly IConfigurationWriter _configurationWriter;
        private readonly IInteractionService _interactionService;

        public DeleteCommand(IConfigurationWriter configurationWriter, IInteractionService interactionService)
            : base("delete", "Delete a configuration value.")
        {
            _configurationWriter = configurationWriter;
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
                var deleted = await _configurationWriter.DeleteConfigurationAsync(key, isGlobal, cancellationToken);
                
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