// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli;

public class Program
{
    private static IHost BuildApplication(ParseResult parseResult)
    {
        var builder = Host.CreateApplicationBuilder();

        var debugOption = parseResult.GetValue<bool>("--debug");

        if (!debugOption)
        {
            // Suppress all logging and rely on AnsiConsole output.
            builder.Logging.AddFilter((_) => false);
        }
        else
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
        }

        builder.Services.AddSingleton<ConsoleAppModel>();
        builder.Services.AddTransient<DotNetCliRunner>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddTransient<INuGetPackageCache, NuGetPackageCache>();
        var app = builder.Build();
        return app;
    }

    private static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand(".NET Aspire CLI");

        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Recursive = true;
        rootCommand.Options.Add(debugOption);
        
        #if DEBUG
        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger", "-w");
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;
        waitForDebuggerOption.Validators.Add((result) => {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                AnsiConsole.Status().Start(
                    $"Waiting for debugger to attach to process ID: {Environment.ProcessId}",
                    context => {
                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                );
            }
        });
        rootCommand.Options.Add(waitForDebuggerOption);
        #endif

        ConfigureRunCommand(rootCommand);
        ConfigureBuildCommand(rootCommand);
        ConfigureNewCommand(rootCommand);
        ConfigureAddCommand(rootCommand);
        return rootCommand;
    }

    private static void ValidateProjectOption(OptionResult result)
    {
        var value = result.GetValueOrDefault<FileInfo?>();

        if (result.Implicit)
        {
            // Having no value here is fine, but there has to
            // be a single csproj file in the current
            // working directory.
            var csprojFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");

            if (csprojFiles.Length > 1)
            {
                result.AddError("The --project option was not specified and multiple *.csproj files were detected.");
                return;
            }
            else if (csprojFiles.Length == 0)
            {
                result.AddError("The --project option was not specified and no *.csproj files were detected.");
                return;
            }

            return;
        }

        if (value is null)
        {
            result.AddError("The --project option was specified but no path was provided.");
            return;
        }

        if (!File.Exists(value.FullName))
        {
            result.AddError("The specified project file does not exist.");
            return;
        }
    }

    private static void ConfigureRunCommand(Command parentCommand)
    {
        var command = new Command("run", "Run a .NET Aspire AppHost project in development mode.");

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var model = app.Services.GetRequiredService<ConsoleAppModel>();
            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);

            var env = new Dictionary<string, string>();

            var debug = parseResult.GetValue<bool>("--debug");
            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");
            var useRichConsole = !debug && !waitForDebugger;

            if (waitForDebugger)
            {
                env["ASPIRE_WAIT_FOR_DEBUGGER"] = "true";
            }

            var pendingRun = runner.RunAsync(effectiveAppHostProjectFile, Array.Empty<string>(), env, ct).ConfigureAwait(false);

            if (useRichConsole)
            {
                // We wait for the first update of the console model via RPC from the AppHost.
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots3)
                    .SpinnerStyle(Style.Parse("purple"))
                    .StartAsync(":linked_paperclips: Starting Aspire app host...", async context => {
                        await model.ModelUpdatedChannel.Reader.ReadAsync(ct).ConfigureAwait(true);
                        }).ConfigureAwait(true);

                // We wait for the first update of the console model via RPC from the AppHost.
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots3)
                    .SpinnerStyle(Style.Parse("purple"))
                    .StartAsync(":chart_increasing: Starting Aspire dashboard...", async context => {

                    // Possible we already have it, if so this will be quick.
                    if (model.DashboardLoginUrl is { })
                    {
                        return;
                    }

                    // Otherwise we wait.
                    while (true)
                    {
                        await model.ModelUpdatedChannel.Reader.ReadAsync(ct).ConfigureAwait(true);
                        if (model.DashboardLoginUrl is { })
                        {
                            break;
                        }
                    }
                    }).ConfigureAwait(true);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[green bold]Dashboard[/]:");
                AnsiConsole.MarkupLine($"[link={model.DashboardLoginUrl}]{model.DashboardLoginUrl}[/]");
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded);

                await AnsiConsole.Live(table).StartAsync(async context => {

                    table.AddColumn("Resource");
                    table.AddColumn("Type");
                    table.AddColumn("State");
                    table.AddColumn("Endpoint(s)");

                    while (true)
                    {
                        var modelUpdate = await model.ModelUpdatedChannel.Reader.ReadAsync(ct).ConfigureAwait(true);
                        table.Rows.Clear();

                        foreach (var resource in model.Resources.OrderBy(r => r.Name))
                        {
                            var resourceName = new Text(resource.Name, new Style().Foreground(Color.White));

                            var type = new Text(resource.Type ?? "Unknown", new Style().Foreground(Color.White));

                            var state = resource.State switch {
                                "Running" => new Text(resource.State, new Style().Foreground(Color.Green)),
                                "Starting" => new Text(resource.State, new Style().Foreground(Color.LightGreen)),
                                "FailedToStart" => new Text(resource.State, new Style().Foreground(Color.Red)),
                                "Waiting" => new Text(resource.State, new Style().Foreground(Color.White)),
                                "Unhealthy" => new Text(resource.State, new Style().Foreground(Color.Yellow)),
                                "Exited" => new Text(resource.State, new Style().Foreground(Color.Grey)),
                                "Finished" => new Text(resource.State, new Style().Foreground(Color.Grey)),
                                "NotStarted" => new Text(resource.State, new Style().Foreground(Color.Grey)),
                                _ => new Text(resource.State ?? "Unknown", new Style().Foreground(Color.Grey))
                            };

                            IRenderable endpoints = new Text("None");
                            if (resource.Endpoints?.Length > 0)
                            {
                                endpoints = new Rows(
                                    resource.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                                );
                            }

                            table.AddRow(resourceName, type, state, endpoints);
                        }

                        context.Refresh();
                    }
                }).ConfigureAwait(true);

                return await pendingRun;
            }
            else
            {
                return await pendingRun;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    private static FileInfo UseOrFindAppHostProjectFile(FileInfo? projectFile)
    {
        if (projectFile is not null)
        {
            // If the project file is passed, just use it.
            return projectFile;
        }

        var projectFilePaths = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");
        try 
        {
            var projectFilePath = projectFilePaths?.SingleOrDefault();
            if (projectFilePath is null)
            {
                throw new InvalidOperationException("No project file found.");
            }
            else
            {
                return new FileInfo(projectFilePath);
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            if (projectFilePaths.Length > 1)
            {
                AnsiConsole.MarkupLine("[red bold]The --project option was not specified and multiple *.csproj files were detected.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red bold]The --project option was not specified and no *.csproj files were detected.[/]");
            }
            return new FileInfo(Environment.CurrentDirectory);
        };
    }

    private static void ConfigureBuildCommand(Command parentCommand)
    {
        var command = new Command("build", "Builds deployment artifacts for a .NET Aspire AppHost project.");

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var targetOption = new Option<string>("--target", "-t");
        command.Options.Add(targetOption);

        var outputPath = new Option<string>("--output-path", "-o");
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            var env = new Dictionary<string, string>();

            if (parseResult.GetValue<bool?>("--wait-for-debugger") ?? false)
            {
                env["ASPIRE_WAIT_FOR_DEBUGGER"] = "true";
            }

            var target = parseResult.GetValue<string>("--target");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath ?? ".");

            var exitCode = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .SpinnerStyle(Style.Parse("purple"))
                .StartAsync(":hammer_and_wrench: Building artifacts...", async context => {
                    var pendingRun = runner.RunAsync(
                        effectiveAppHostProjectFile,
                        ["--publisher", target ?? "manifest", "--output-path", fullyQualifiedOutputPath],
                        env,
                        ct).ConfigureAwait(false);

                    return await pendingRun;
                }).ConfigureAwait(false);

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold] :thumbs_down: The build failed with exit code {exitCode}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green bold] :thumbs_up: The build completed successfully to: {fullyQualifiedOutputPath}[/]");
                return ExitCodeConstants.Success;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    private static void ValidateProjectTemplate(ArgumentResult result)
    {
        // TODO: We need to integrate with the template engine to interrogate
        //       the list of available templates. For now we will just hard-code
        //       the acceptable options.
        //
        //       Once we integrate with template engine we will also be able to
        //       interrogate the various options and add them. For now we will 
        //       keep it simple.
        string[] validTemplates = [
            "aspire-starter",
            "aspire",
            "aspire-apphost",
            "aspire-servicedefaults",
            "aspire-mstest",
            "aspire-nunit",
            "aspire-xunit"
            ];

        var value = result.GetValueOrDefault<string>();

        if (value is null)
        {
            // This is OK, for now we will use the default
            // template of aspire-starter, but we might
            // be able to do more intelligent selection in the
            // future based on what is already in the working directory.
            return;
        }

        if (value is { } templateName && !validTemplates.Contains(templateName))
        {
            result.AddError($"The specified template '{templateName}' is not valid. Valid templates are [{string.Join(", ", validTemplates)}].");
            return;
        }
    }

    private static void ConfigureNewCommand(Command parentCommand)
    {
        var command = new Command("new", "Create a new .NET Aspire-related project.");
        var templateArgument = new Argument<string>("template");
        templateArgument.Validators.Add(ValidateProjectTemplate);
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        command.Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        command.Options.Add(outputOption);

        var templateVersionOption = new Option<string?>("--template-version", "-v");
        templateVersionOption.DefaultValueFactory = (result) => "9.2.0"; // HACK: We should make it use the version that matches the CLI.
        command.Options.Add(templateVersionOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var templateVersion = parseResult.GetValue<string>("--template-version");

            var cliRunner = app.Services.GetRequiredService<DotNetCliRunner>();
            var templateInstallExitCode = await cliRunner.InstallTemplateAsync("Aspire.ProjectTemplates", templateVersion!, true, ct).ConfigureAwait(false);

            if (templateInstallExitCode != 0)
            {
                return ExitCodeConstants.FailedToInstallTemplates;
            }

            var templateName = parseResult.GetValue<string>("template") ?? "aspire-starter";

            if (parseResult.GetValue<string>("--output") is not { } outputPath)
            {
                outputPath = Environment.CurrentDirectory;
            }
            else
            {
                outputPath = Path.GetFullPath(outputPath);
            }

            if (parseResult.GetValue<string>("--name") is not { } name)
            {
                var outputPathDirectoryInfo = new DirectoryInfo(outputPath);
                name = outputPathDirectoryInfo.Name;
            }

            var newProjectExitCode = await cliRunner.NewProjectAsync(
                templateName,
                name,
                outputPath,
                ct).ConfigureAwait(false);

            if (newProjectExitCode != 0)
            {
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            return 0;
        });

        parentCommand.Subcommands.Add(command);
    }

    private static (string FriendlyName, NuGetPackage Package) GetPackageByInteractiveFlow(IEnumerable<(string FriendlyName, NuGetPackage Package)> knownPackages)
    {
        var packagePrompt = new SelectionPrompt<(string FriendlyName, NuGetPackage Package)>()
            .Title("Please select the integration you want to add:")
            .UseConverter(PackageNameWithFriendlyNameIfAvailable)
            .PageSize(10)
            .AddChoices(knownPackages);

        var selectedIntegration = AnsiConsole.Prompt(packagePrompt);

        var versionPrompt = new TextPrompt<string>("Specify version of integration to add:")
            .DefaultValue(selectedIntegration.Package.Version)
            .Validate(value => string.IsNullOrEmpty(value) ? ValidationResult.Error("Version cannot be empty.") : ValidationResult.Success())
            .ShowDefaultValue(true);

        var version = AnsiConsole.Prompt(versionPrompt);

        selectedIntegration.Package.Version = version;

        return selectedIntegration;

        static string PackageNameWithFriendlyNameIfAvailable((string FriendlyName, NuGetPackage Package) packageWithFriendlyName)
        {
            if (packageWithFriendlyName.FriendlyName is { } friendlyName)
            {
                return $"{packageWithFriendlyName.Package.Id} ({friendlyName})";
            }
            else
            {
                return packageWithFriendlyName.Package.Id;
            }
        }
    }

    private static (string FriendlyName, NuGetPackage Package) GenerateFriendlyName(NuGetPackage package)
    {
        var shortNameBuilder = new StringBuilder();

        if (package.Id.StartsWith("Aspire.Hosting.Azure."))
        {
            shortNameBuilder.Append("az-");
        }
        else if (package.Id.StartsWith("Aspire.Hosting.AWS."))
        {
            shortNameBuilder.Append("aws-");
        }
        else if (package.Id.StartsWith("CommunityToolkit.Aspire.Hosting."))
        {
            shortNameBuilder.Append("ct-");
        }

        var lastSegment = package.Id.Split('.').Last().ToLower();
        shortNameBuilder.Append(lastSegment);
        return (shortNameBuilder.ToString(), package);
    }

    private static void ConfigureAddCommand(Command parentCommand)
    {
        var command = new Command("add", "Add a resource to the .NET Aspire project.");

        var resourceArgument = new Argument<string>("resource");
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var versionOption = new Option<string>("--version", "-v");
        command.Options.Add(versionOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        command.Options.Add(prereleaseOption);

        command.SetAction(async (parseResult, ct) => {
            try
            {
                var app = BuildApplication(parseResult);
                
                var integrationLookup = app.Services.GetRequiredService<INuGetPackageCache>();

                var integrationName = parseResult.GetValue<string>("resource");

                var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
                var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);

                var prerelease = parseResult.GetValue<bool>("--prerelease");

                var packages = await AnsiConsole.Status().StartAsync(
                    "Searching for Aspire packages...",
                    context => integrationLookup.GetPackagesAsync(effectiveAppHostProjectFile, prerelease, ct)
                    ).ConfigureAwait(false);

                var packagesWithShortName = packages.Select(p => GenerateFriendlyName(p));

                var selectedNuGetPackage = packagesWithShortName.FirstOrDefault(p => p.FriendlyName == integrationName || p.Package.Id == integrationName);

                if (selectedNuGetPackage == default)
                {
                    selectedNuGetPackage = GetPackageByInteractiveFlow(packagesWithShortName);
                }
                else
                {
                    // If we find an exact match we will use it, but override the version
                    // if the version option is specified.
                    var version = parseResult.GetValue<string?>("--version");
                    if (version is not null)
                    {
                        selectedNuGetPackage.Package.Version = version;
                    }
                }

                var addPackageResult = await AnsiConsole.Status().StartAsync(
                    "Adding Aspire package...",
                    async context => {
                        var runner = app.Services.GetRequiredService<DotNetCliRunner>();
                        var addPackageResult = await runner.AddPackageAsync(
                            effectiveAppHostProjectFile,
                            selectedNuGetPackage.Package.Id,
                            selectedNuGetPackage.Package.Version,
                            ct
                            ).ConfigureAwait(false);

                        return addPackageResult == 0 ? ExitCodeConstants.Success : ExitCodeConstants.FailedToAddPackage;
                    }                
                ).ConfigureAwait(false);

                return addPackageResult;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red bold]An error occured while adding the package: {ex.Message}[/]");
                return ExitCodeConstants.FailedToAddPackage;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    public static async Task<int> Main(string[] args)
    {
        System.Console.OutputEncoding = Encoding.UTF8;
        var rootCommand = GetRootCommand();
        var result = rootCommand.Parse(args);
        var exitCode = await result.InvokeAsync().ConfigureAwait(false);
        return exitCode;
    }
}