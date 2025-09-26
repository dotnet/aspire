// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Rosetta;
using Aspire.Cli.Utils;
using Rosetta;

namespace Aspire.Cli.Commands;

internal sealed class PolyglotCommand : BaseCommand
{
    public PolyglotCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, RunCommand runCommand) :
        base("polyglot", PolyglotCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        var newCommand = new NewCommand(InteractionService, features, updateNotifier, executionContext);
        Subcommands.Add(newCommand);

        var serveCommand = new ServeCommand(InteractionService, features, updateNotifier, executionContext, runCommand);
        Subcommands.Add(serveCommand);

        var addCommand = new AddCommand(InteractionService, features, updateNotifier, executionContext);
        Subcommands.Add(addCommand);

        var polyRunCommand = new PolyRunCommand(InteractionService, features, updateNotifier, executionContext);
        Subcommands.Add(polyRunCommand);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        new HelpAction().Invoke(parseResult);
        return Task.FromResult(ExitCodeConstants.InvalidCommand);
    }

    public enum Languages
    {
        TypeScript,
        Python,
    }

    private sealed class NewCommand : BaseCommand
    {
        public NewCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("new", PolyglotCommandStrings.NewCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
            var nameOption = new Option<Languages?>("--lang", "-l");
            nameOption.Description = PolyglotCommandStrings.NewCommand_LangArgumentDescription;
            Options.Add(nameOption);

            var outputOption = new Option<string?>("--output", "-o");
            outputOption.Description = PolyglotCommandStrings.OutputArgumentDescription;
            Options.Add(outputOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            try
            {
                var language = parseResult.GetValue<Languages?>("--lang") ?? Languages.TypeScript;
                var output = parseResult.GetValue<string?>("--output") ?? ExecutionContext.WorkingDirectory.FullName;

                output = Path.Combine(ExecutionContext.WorkingDirectory.FullName, new DirectoryInfo(output).FullName);

                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                var appModel = await RosettaServices.CreateApplicationModel(output, InteractionService);

                var codegen = RosettaServices.CreateCodegenerator(appModel, language);

                codegen.GenerateDistributedApplication();

                codegen.GenerateAppHost(output);

                // TODO: We may need to run `npm install` to install the dependencies
                // Could be a message in the console or do it automatically

                InteractionService.DisplaySuccess($"New application created in {new Uri(output).LocalPath}.");

                return ExitCodeConstants.Success;
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, PolyglotCommandStrings.NewCommand_Failed, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }

    private sealed class ServeCommand : BaseCommand
    {
        private readonly RunCommand _runCommand;

        public ServeCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, RunCommand runCommand)
            : base("serve", PolyglotCommandStrings.ServeCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
            _runCommand = runCommand;

            var outputOption = new Option<string?>("--output", "-o");
            outputOption.Description = PolyglotCommandStrings.OutputArgumentDescription;
            Options.Add(outputOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var output = parseResult.GetValue<string?>("--output") ?? ExecutionContext.WorkingDirectory.FullName;

            output = Path.Combine(ExecutionContext.WorkingDirectory.FullName, new DirectoryInfo(output).FullName);

            try
            {
                var projectModel = new ProjectModel(output);

                var passedAppHostProjectFile = new FileInfo(Path.Combine(projectModel.ProjectModelPath, "RosettaHost.csproj"));

                return await _runCommand.ExecuteInternalAsync(passedAppHostProjectFile, false, false, false, [], cancellationToken);
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, PolyglotCommandStrings.ServeCommand_Failed, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }

    private sealed class AddCommand : BaseCommand
    {
        public AddCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("add", PolyglotCommandStrings.AddCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
            var packageArgument = new Argument<string>("package");
            packageArgument.Description = PolyglotCommandStrings.PackageOptionDescription;
            packageArgument.Arity = ArgumentArity.ExactlyOne;
            Arguments.Add(packageArgument);

            var versionOption = new Option<string>("--version", "-v");
            versionOption.Description = PolyglotCommandStrings.VersionOptionDescription;
            versionOption.Required = true;
            Options.Add(versionOption);

            var outputOption = new Option<string?>("--output", "-o");
            outputOption.Description = PolyglotCommandStrings.OutputArgumentDescription;
            Options.Add(outputOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var packageName = parseResult.GetValue<string>("package")!;
            var packageVersion = parseResult.GetValue<string>("--version");
            var output = parseResult.GetValue<string?>("--output") ?? ExecutionContext.WorkingDirectory.FullName;

            output = Path.Combine(ExecutionContext.WorkingDirectory.FullName, new DirectoryInfo(output).FullName);

            try
            {
                var packagesJson = PackagesJson.Open(output);

                if (PackagesJson.GetPackageByShortName(packageName) is { } reference)
                {
                    (packageName, packageVersion) = reference;
                }

                ArgumentException.ThrowIfNullOrEmpty(packageVersion);

                packagesJson.Import(packageName, packageVersion);

                var appModel = await RosettaServices.CreateApplicationModel(output, InteractionService);

                // Detect language from existing app
                var codegen = RosettaServices.CreateCodegenerator(appModel);

                codegen.GenerateDistributedApplication();

                codegen.GenerateAppHost(output);

                InteractionService.DisplaySuccess($"New dependency added");

                return ExitCodeConstants.Success;
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, PolyglotCommandStrings.AddCommand_Failed, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }

    private sealed class PolyRunCommand : BaseCommand
    {
        public PolyRunCommand(IInteractionService interactionService, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext)
            : base("run", PolyglotCommandStrings.RunCommand_Description, features, updateNotifier, executionContext, interactionService)
        {
            var outputOption = new Option<string?>("--output", "-o");
            outputOption.Description = PolyglotCommandStrings.OutputArgumentDescription;
            Options.Add(outputOption);
        }

        protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var output = parseResult.GetValue<string?>("--output") ?? ExecutionContext.WorkingDirectory.FullName;

            output = Path.Combine(ExecutionContext.WorkingDirectory.FullName, new DirectoryInfo(output).FullName);

            try
            {
                var appModel = await RosettaServices.CreateApplicationModel(output, InteractionService);

                // Detect language
                var codegen = RosettaServices.CreateCodegenerator(appModel);

                codegen.ExecuteAppHost(output);

                return ExitCodeConstants.Success;
            }
            catch (Exception ex)
            {
                InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, PolyglotCommandStrings.RunCommand_Failed, ex.Message));
                return ExitCodeConstants.InvalidCommand;
            }
        }
    }
}
