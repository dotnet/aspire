// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class ConfigCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IInteractionService _interactionService;

    public ConfigCommand(IConfiguration configuration, IConfigurationService configurationService, IInteractionService interactionService, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : base("config", ConfigCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(sdkInstaller);

        _configuration = configuration;
        _interactionService = interactionService;

        var getCommand = new GetCommand(configurationService, InteractionService, features, updateNotifier, executionContext);
        var setCommand = new SetCommand(configurationService, InteractionService, features, updateNotifier, executionContext);
        var listCommand = new ListCommand(configurationService, InteractionService, features, updateNotifier, executionContext);
        var deleteCommand = new DeleteCommand(configurationService, InteractionService, features, updateNotifier, executionContext);

        Subcommands.Add(getCommand);
        Subcommands.Add(setCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(deleteCommand);
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
        var subcommand = await _interactionService.PromptForSelectionAsync(
            ConfigCommandStrings.ExtensionActionPrompt,
            Subcommands.Cast<BaseConfigSubCommand>(),
            cmd =>
            {
                Debug.Assert(cmd.Description is not null);
                return cmd.Description.TrimEnd('.');
            },
            cancellationToken);

        return await subcommand.InteractiveExecuteAsync(cancellationToken);
    }

    private sealed class GetCommand : BaseConfigSubCommand
    {
        public GetCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("get", ConfigCommandStrings.GetCommand_Description, features, updateNotifier, configurationService, executionContext, interactionService)
        {
            var keyArgument = new Argument<string>("key")
            {
                Description = ConfigCommandStrings.GetCommand_KeyArgumentDescription
            };
            Arguments.Add(keyArgument);
        }

        protected override bool UpdateNotificationsEnabled => false;

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            if (key is null)
            {
                InteractionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }

            return ExecuteAsync(key, cancellationToken);
        }

        public override async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
        {
            var key = await InteractionService.PromptForStringAsync(ConfigCommandStrings.GetCommand_PromptForKey, required: true, cancellationToken: cancellationToken);
            return await ExecuteAsync(key, cancellationToken);
        }

        private async Task<int> ExecuteAsync(string key, CancellationToken cancellationToken)
        {
            var value = await ConfigurationService.GetConfigurationAsync(key, cancellationToken);

            if (value is not null)
            {
                InteractionService.DisplayPlainText(value);
                return ExitCodeConstants.Success;
            }
            else
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationKeyNotFound, key));
                return ExitCodeConstants.ConfigNotFound;
            }
        }
    }

    private sealed class SetCommand : BaseConfigSubCommand
    {
        public SetCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("set", ConfigCommandStrings.SetCommand_Description, features, updateNotifier, configurationService, executionContext, interactionService)
        {
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

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var value = parseResult.GetValue<string>("value");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                InteractionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }

            if (value is null)
            {
                InteractionService.DisplayError(ErrorStrings.ConfigurationValueRequired);
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }

            return ExecuteAsync(key, value, isGlobal, cancellationToken);
        }

        public override async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
        {
            var key = await InteractionService.PromptForStringAsync(ConfigCommandStrings.SetCommand_PromptForKey, required: true, cancellationToken: cancellationToken);
            var value = await InteractionService.PromptForStringAsync(ConfigCommandStrings.SetCommand_PromptForValue, required: true, cancellationToken: cancellationToken);
            var isGlobal = await InteractionService.PromptForSelectionAsync(
                ConfigCommandStrings.SetCommand_PromptForGlobal,
                [false, true],
                g => g ? ConfigCommandStrings.SetCommand_PromptForGlobal_GlobalOption : ConfigCommandStrings.SetCommand_PromptForGlobal_LocalOption,
                cancellationToken: cancellationToken);

            return await ExecuteAsync(key, value, isGlobal, cancellationToken);
        }

        private async Task<int> ExecuteAsync(string key, string value, bool isGlobal, CancellationToken cancellationToken)
        {
            try
            {
                await ConfigurationService.SetConfigurationAsync(key, value, isGlobal, cancellationToken);
                InteractionService.DisplaySuccess(isGlobal
                    ? string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeySetGlobally, key,
                        value)
                    : string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeySetLocally, key,
                        value));

                return ExitCodeConstants.Success;
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ErrorSettingConfiguration, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }

    private sealed class ListCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
        : BaseConfigSubCommand("list", ConfigCommandStrings.ListCommand_Description, features, updateNotifier, configurationService, executionContext, interactionService)
    {
        protected override bool UpdateNotificationsEnabled => false;

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            return InteractiveExecuteAsync(cancellationToken);
        }

        public override async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
        {
            if (InteractionService is ExtensionInteractionService extensionInteractionService)
            {
                var settingsFilePath = ConfigurationService.GetSettingsFilePath(isGlobal: false);
                if (Path.Exists(settingsFilePath))
                {
                    extensionInteractionService.OpenEditor(settingsFilePath);
                    return ExitCodeConstants.Success;
                }
            }

            var localConfig = await ConfigurationService.GetLocalConfigurationAsync(cancellationToken);
            var globalConfig = await ConfigurationService.GetGlobalConfigurationAsync(cancellationToken);

            // Check if we have any configuration at all
            if (localConfig.Count == 0 && globalConfig.Count == 0)
            {
                InteractionService.DisplayMessage("information", ConfigCommandStrings.NoConfigurationValuesFound);
                return ExitCodeConstants.Success;
            }

            var featurePrefix = $"{KnownFeatures.FeaturePrefix}.";

            // Separate features from other config
            var localFeatures = localConfig.Where(kvp => kvp.Key.StartsWith(featurePrefix, StringComparison.Ordinal)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var localNonFeatures = localConfig.Where(kvp => !kvp.Key.StartsWith(featurePrefix, StringComparison.Ordinal)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var globalFeatures = globalConfig.Where(kvp => kvp.Key.StartsWith(featurePrefix, StringComparison.Ordinal)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var globalNonFeatures = globalConfig.Where(kvp => !kvp.Key.StartsWith(featurePrefix, StringComparison.Ordinal)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Display Local Configuration (non-features)
            if (localNonFeatures.Count > 0)
            {
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.LocalConfigurationHeader}:**");
                foreach (var kvp in localNonFeatures.OrderBy(k => k.Key))
                {
                    InteractionService.DisplayMarkdown($"  [cyan]{kvp.Key.EscapeMarkup()}[/] = [yellow]{kvp.Value.EscapeMarkup()}[/]");
                }
            }
            else if (globalNonFeatures.Count > 0)
            {
                // Only show "no local config" message if we have global config
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.LocalConfigurationHeader}:**");
                InteractionService.DisplaySubtleMessage($"  {ConfigCommandStrings.NoLocalConfigurationFound}");
            }

            // Display Global Configuration (non-features)
            if (globalNonFeatures.Count > 0)
            {
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.GlobalConfigurationHeader}:**");
                foreach (var kvp in globalNonFeatures.OrderBy(k => k.Key))
                {
                    InteractionService.DisplayMarkdown($"  [cyan]{kvp.Key.EscapeMarkup()}[/] = [yellow]{kvp.Value.EscapeMarkup()}[/]");
                }
            }
            else if (localNonFeatures.Count > 0)
            {
                // Only show "no global config" message if we have local config
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.GlobalConfigurationHeader}:**");
                InteractionService.DisplaySubtleMessage($"  {ConfigCommandStrings.NoGlobalConfigurationFound}");
            }

            // Display Configured Features
            var allConfiguredFeatures = localFeatures.Concat(globalFeatures.Where(gf => !localFeatures.ContainsKey(gf.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (allConfiguredFeatures.Count > 0)
            {
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.ConfiguredFeaturesHeader}:**");
                foreach (var kvp in allConfiguredFeatures.OrderBy(k => k.Key))
                {
                    var featureName = kvp.Key.Substring(featurePrefix.Length);
                    var isEnabled = kvp.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    var statusColor = isEnabled ? "green" : "red";
                    var statusText = isEnabled ? "enabled" : "disabled";
                    InteractionService.DisplayMarkdown($"  [cyan]{featureName.EscapeMarkup()}[/] = [{statusColor}]{statusText}[/]");
                }
            }

            // Display Available Features
            var availableFeatures = KnownFeatures.GetAllFeatureNames().ToList();
            var configuredFeatureNames = allConfiguredFeatures.Keys.Select(k => k.Substring(featurePrefix.Length)).ToHashSet(StringComparer.Ordinal);
            var unconfiguredFeatures = availableFeatures.Where(f => !configuredFeatureNames.Contains(f)).ToList();

            if (unconfiguredFeatures.Count > 0)
            {
                InteractionService.DisplayEmptyLine();
                InteractionService.DisplayMarkdown($"**{ConfigCommandStrings.AvailableFeaturesHeader}:**");
                InteractionService.DisplaySubtleMessage($"  {string.Join(", ", unconfiguredFeatures)}");
            }

            InteractionService.DisplayEmptyLine();
            return ExitCodeConstants.Success;
        }
    }

    private sealed class DeleteCommand : BaseConfigSubCommand
    {
        public DeleteCommand(IConfigurationService configurationService, IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("delete", ConfigCommandStrings.DeleteCommand_Description, features, updateNotifier, configurationService, executionContext, interactionService)
        {
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

        protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue<string>("key");
            var isGlobal = parseResult.GetValue<bool>("--global");

            if (key is null)
            {
                InteractionService.DisplayError(ErrorStrings.ConfigurationKeyRequired);
                return Task.FromResult(ExitCodeConstants.InvalidCommand);
            }

            return ExecuteAsync(key, isGlobal, cancellationToken);
        }

        public override async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
        {
            var key = await InteractionService.PromptForStringAsync(ConfigCommandStrings.DeleteCommand_PromptForKey, required: true, cancellationToken: cancellationToken);

            var value = await ConfigurationService.GetConfigurationAsync(key, cancellationToken);
            if (value is null)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationKeyNotFound, key));
                return ExitCodeConstants.ConfigNotFound;
            }

            var isGlobal = await InteractionService.PromptForSelectionAsync(
                ConfigCommandStrings.DeleteCommand_PromptForGlobal,
                [false, true],
                g => g ? TemplatingStrings.Yes : TemplatingStrings.No,
                cancellationToken: cancellationToken);

            return await ExecuteAsync(key, isGlobal, cancellationToken);
        }

        private async Task<int> ExecuteAsync(string key, bool isGlobal, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await ConfigurationService.DeleteConfigurationAsync(key, isGlobal, cancellationToken);

                if (deleted)
                {
                    if (isGlobal)
                    {
                        InteractionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeyDeletedGlobally, key));
                    }
                    else
                    {
                        InteractionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ConfigCommandStrings.ConfigurationKeyDeletedLocally, key));
                    }

                    return ExitCodeConstants.Success;
                }
                else
                {
                    InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationKeyNotFound, key));
                    return ExitCodeConstants.InvalidCommand;
                }
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.ErrorDeletingConfiguration, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }
}
