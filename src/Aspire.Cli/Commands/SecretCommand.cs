// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Secrets;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Manages AppHost user secrets (set, get, list, delete).
/// </summary>
internal sealed class SecretCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.ToolsAndConfiguration;

    public SecretCommand(
        IInteractionService interactionService,
        SecretStoreResolver resolver,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("secret", "Manage AppHost user secrets.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        var setCommand = new SetCommand(interactionService, resolver);
        var getCommand = new GetCommand(interactionService, resolver);
        var listCommand = new ListCommand(interactionService, resolver);
        var deleteCommand = new DeleteCommand(interactionService, resolver);

        Subcommands.Add(setCommand);
        Subcommands.Add(getCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(deleteCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = "The path to the Aspire AppHost project file."
    };

    private sealed class SetCommand : Command
    {
        private static readonly Argument<string> s_keyArgument = new("key")
        {
            Description = "The secret key (e.g., Azure:Location or Parameters:postgres-password)."
        };

        private static readonly Argument<string> s_valueArgument = new("value")
        {
            Description = "The secret value to set."
        };

        private readonly IInteractionService _interactionService;
        private readonly SecretStoreResolver _resolver;

        public SetCommand(IInteractionService interactionService, SecretStoreResolver resolver)
            : base("set", "Set a secret value.")
        {
            _interactionService = interactionService;
            _resolver = resolver;

            Arguments.Add(s_keyArgument);
            Arguments.Add(s_valueArgument);
            Options.Add(s_projectOption);

            this.SetAction(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue(s_keyArgument)!;
            var value = parseResult.GetValue(s_valueArgument)!;
            var projectFile = parseResult.GetValue(s_projectOption);

            var result = await _resolver.ResolveAsync(projectFile, autoInit: true, cancellationToken);
            if (result is null)
            {
                _interactionService.DisplayError("Could not find an AppHost project.");
                return ExitCodeConstants.FailedToFindProject;
            }

            result.Store.Set(key, value);
            result.Store.Save();

            _interactionService.DisplaySuccess($"Secret '{key}' set successfully.");
            return ExitCodeConstants.Success;
        }
    }

    private sealed class GetCommand : Command
    {
        private static readonly Argument<string> s_keyArgument = new("key")
        {
            Description = "The secret key to retrieve."
        };

        private readonly IInteractionService _interactionService;
        private readonly SecretStoreResolver _resolver;

        public GetCommand(IInteractionService interactionService, SecretStoreResolver resolver)
            : base("get", "Get a secret value.")
        {
            _interactionService = interactionService;
            _resolver = resolver;

            Arguments.Add(s_keyArgument);
            Options.Add(s_projectOption);

            this.SetAction(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue(s_keyArgument)!;
            var projectFile = parseResult.GetValue(s_projectOption);

            var result = await _resolver.ResolveAsync(projectFile, autoInit: false, cancellationToken);
            if (result is null)
            {
                _interactionService.DisplayError("Could not find an AppHost project.");
                return ExitCodeConstants.FailedToFindProject;
            }

            var value = result.Store.Get(key);
            if (value is null)
            {
                _interactionService.DisplayError($"Secret '{key}' not found.");
                return ExitCodeConstants.ConfigNotFound;
            }

            // Write value to stdout (machine-readable)
            _interactionService.DisplayPlainText(value);
            return ExitCodeConstants.Success;
        }
    }

    private sealed class ListCommand : Command
    {
        private static readonly Option<OutputFormat?> s_formatOption = new("--format")
        {
            Description = "Output format."
        };

        private readonly IInteractionService _interactionService;
        private readonly SecretStoreResolver _resolver;

        public ListCommand(IInteractionService interactionService, SecretStoreResolver resolver)
            : base("list", "List all secrets.")
        {
            _interactionService = interactionService;
            _resolver = resolver;

            Options.Add(s_projectOption);
            Options.Add(s_formatOption);

            this.SetAction(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var projectFile = parseResult.GetValue(s_projectOption);
            var format = parseResult.GetValue(s_formatOption);

            var result = await _resolver.ResolveAsync(projectFile, autoInit: false, cancellationToken);
            if (result is null)
            {
                _interactionService.DisplayError("Could not find an AppHost project.");
                return ExitCodeConstants.FailedToFindProject;
            }

            var secrets = result.Store.AsEnumerable().ToList();

            if (format == OutputFormat.Json)
            {
                var obj = new JsonObject();
                foreach (var (key, value) in secrets)
                {
                    obj[key] = value;
                }

                var json = obj.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                // Structured output always goes to stdout.
                _interactionService.DisplayRawText(json, ConsoleOutput.Standard);
            }
            else
            {
                if (secrets.Count == 0)
                {
                    _interactionService.DisplayMessage("information", "No secrets configured.");
                }
                else
                {
                    foreach (var (key, value) in secrets.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        _interactionService.DisplayPlainText($"{key} = {value}");
                    }
                }
            }

            return ExitCodeConstants.Success;
        }
    }

    private sealed class DeleteCommand : Command
    {
        private static readonly Argument<string> s_keyArgument = new("key")
        {
            Description = "The secret key to delete."
        };

        private readonly IInteractionService _interactionService;
        private readonly SecretStoreResolver _resolver;

        public DeleteCommand(IInteractionService interactionService, SecretStoreResolver resolver)
            : base("delete", "Delete a secret.")
        {
            _interactionService = interactionService;
            _resolver = resolver;

            Arguments.Add(s_keyArgument);
            Options.Add(s_projectOption);

            this.SetAction(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var key = parseResult.GetValue(s_keyArgument)!;
            var projectFile = parseResult.GetValue(s_projectOption);

            var result = await _resolver.ResolveAsync(projectFile, autoInit: false, cancellationToken);
            if (result is null)
            {
                _interactionService.DisplayError("Could not find an AppHost project.");
                return ExitCodeConstants.FailedToFindProject;
            }

            if (!result.Store.Remove(key))
            {
                _interactionService.DisplayError($"Secret '{key}' not found.");
                return ExitCodeConstants.ConfigNotFound;
            }

            result.Store.Save();
            _interactionService.DisplaySuccess($"Secret '{key}' deleted successfully.");
            return ExitCodeConstants.Success;
        }
    }
}
