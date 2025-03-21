// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Data;
using System.Diagnostics;
using System.Text;
using Aspire.Cli.Backchannel;
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

        builder.Services.AddTransient<DotNetCliRunner>();
        builder.Services.AddTransient<AppHostBackchannel>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddTransient<INuGetPackageCache, NuGetPackageCache>();
        var app = builder.Build();
        return app;
    }

    private static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand("Aspire CLI");

        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Recursive = true;
        rootCommand.Options.Add(debugOption);
        
        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger", "-w");
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;

        #if DEBUG
        waitForDebuggerOption.Validators.Add((result) => {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                AnsiConsole.Status().Start(
                    $":bug:  Waiting for debugger to attach to process ID: {Environment.ProcessId}",
                    context => {
                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                );
            }
        });
        #endif

        rootCommand.Options.Add(waitForDebuggerOption);

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

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct);

            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            var debug = parseResult.GetValue<bool>("--debug");

            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");

            var forceUseRichConsole = Environment.GetEnvironmentVariable("ASPIRE_FORCE_RICH_CONSOLE") == "true";
            
            var useRichConsole = forceUseRichConsole || !debug && !waitForDebugger;

            if (waitForDebugger)
            {
                env["ASPIRE_WAIT_FOR_DEBUGGER"] = "true";
            }

            var backchannelCompletitionSource = new TaskCompletionSource<AppHostBackchannel>();

            var pendingRun = runner.RunAsync(
                effectiveAppHostProjectFile,
                Array.Empty<string>(),
                env,
                backchannelCompletitionSource,
                ct);

            if (useRichConsole)
            {
                // We wait for the back channel to be created to signal that
                // the AppHost is ready to accept requests.
                var backchannel = await AnsiConsole.Status()
                                                   .Spinner(Spinner.Known.Dots3)
                                                   .SpinnerStyle(Style.Parse("purple"))
                                                   .StartAsync(":linked_paperclips:  Starting Aspire app host...", async context => {
                                                        return await backchannelCompletitionSource.Task;
                                                   });

                // We wait for the first update of the console model via RPC from the AppHost.
                var dashboardUrls = await AnsiConsole.Status()
                                                    .Spinner(Spinner.Known.Dots3)
                                                    .SpinnerStyle(Style.Parse("purple"))
                                                    .StartAsync(":chart_increasing:  Starting Aspire dashboard...", async context => {
                                                        return await backchannel.GetDashboardUrlsAsync(ct);
                                                    });

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[green bold]Dashboard[/]:");
                AnsiConsole.MarkupLine($":chart_increasing:  Direct: [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
                if (dashboardUrls.CodespacesUrlWithLoginToken is not  null)
                {
                    AnsiConsole.MarkupLine($":chart_increasing:  Codespaces: [link={dashboardUrls.CodespacesUrlWithLoginToken}]{dashboardUrls.CodespacesUrlWithLoginToken}[/]");
                }
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded);

                await AnsiConsole.Live(table).StartAsync(async context => {

                    var knownResources = new SortedDictionary<string, (string Resource, string Type, string State, string[] Endpoints)>();

                    table.AddColumn("Resource");
                    table.AddColumn("Type");
                    table.AddColumn("State");
                    table.AddColumn("Endpoint(s)");

                    var resourceStates = backchannel.GetResourceStatesAsync(ct);

                    await foreach(var resourceState in resourceStates)
                    {
                        knownResources[resourceState.Resource] = resourceState;

                        table.Rows.Clear();

                        foreach (var knownResource in knownResources)
                        {
                            var nameRenderable = new Text(knownResource.Key, new Style().Foreground(Color.White));

                            var typeRenderable = new Text(knownResource.Value.Type, new Style().Foreground(Color.White));

                            var stateRenderable = knownResource.Value.State switch {
                                "Running" => new Text(knownResource.Value.State, new Style().Foreground(Color.Green)),
                                "Starting" => new Text(knownResource.Value.State, new Style().Foreground(Color.LightGreen)),
                                "FailedToStart" => new Text(knownResource.Value.State, new Style().Foreground(Color.Red)),
                                "Waiting" => new Text(knownResource.Value.State, new Style().Foreground(Color.White)),
                                "Unhealthy" => new Text(knownResource.Value.State, new Style().Foreground(Color.Yellow)),
                                "Exited" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                "Finished" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                "NotStarted" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                _ => new Text(knownResource.Value.State ?? "Unknown", new Style().Foreground(Color.Grey))
                            };

                            IRenderable endpointsRenderable = new Text("None");
                            if (knownResource.Value.Endpoints?.Length > 0)
                            {
                                endpointsRenderable = new Rows(
                                    knownResource.Value.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                                );
                            }

                            table.AddRow(nameRenderable, typeRenderable, stateRenderable, endpointsRenderable);

                        }

                        context.Refresh();
                    }

                });

                return await pendingRun;
            }
            else
            {
                return await pendingRun;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    private static FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
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
                AnsiConsole.MarkupLine("[red bold]:police_car_light: The --project option was not specified and multiple *.csproj files were detected.[/]");
                
            }
            else
            {
                AnsiConsole.MarkupLine("[red bold]:police_car_light: The --project option was not specified and no *.csproj files were detected.[/]");
            }
            return null;
        };
    }

    private static void ConfigureBuildCommand(Command parentCommand)
    {
        var command = new Command("build", "Builds deployment artifacts for a .NET Aspire AppHost project.");

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var publisherOption = new Option<string>("--publisher", "-p");
        command.Options.Add(publisherOption);

        var outputPath = new Option<string>("--output-path", "-o");
        outputPath.DefaultValueFactory = (result) => Path.Combine(Environment.CurrentDirectory);
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct);

            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            if (parseResult.GetValue<bool?>("--wait-for-debugger") ?? false)
            {
                env["ASPIRE_WAIT_FOR_DEBUGGER"] = "true";
            }

            var publisher = parseResult.GetValue<string>("--publisher");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath ?? ".");

            var exitCode = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .SpinnerStyle(Style.Parse("purple"))
                .StartAsync($":hammer_and_wrench:  Building artifacts for '{publisher}' publisher...", async context => {
                    var pendingRun = runner.RunAsync(
                        effectiveAppHostProjectFile,
                        ["--publisher", publisher ?? "manifest", "--output-path", fullyQualifiedOutputPath],
                        env,
                        null, // TODO: We will use a backchannel here soon but null for now.
                        ct);

                    return await pendingRun;
                });

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  The build failed with exit code {exitCode}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green bold]:thumbs_up:  The build completed successfully to: {fullyQualifiedOutputPath}[/]");
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
        var command = new Command("new", "Create a new Aspire sample project.");
        var templateArgument = new Argument<string>("template");
        templateArgument.Validators.Add(ValidateProjectTemplate);
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        command.Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        command.Options.Add(outputOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        command.Options.Add(prereleaseOption);
        
        var sourceOption = new Option<string?>("--source", "-s");
        command.Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        templateVersionOption.DefaultValueFactory = (result) => 
        {
            if (result.GetValue<bool>("--prerelease"))
            {
                return "*-*";
            }
            else
            {
                return "9.2.0"; // HACK: Need to get this from the CLI version.
            }
        };
        command.Options.Add(templateVersionOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            var cliRunner = app.Services.GetRequiredService<DotNetCliRunner>();
            _ = app.RunAsync(ct);

            var templateVersion = parseResult.GetValue<string>("--version");
            var source = parseResult.GetValue<string?>("--source");

            int templateInstallExitCode = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .SpinnerStyle(Style.Parse("purple"))
                .StartAsync(
                    ":ice:  Getting latest templates...",
                    async context => {
                        return await cliRunner.InstallTemplateAsync("Aspire.ProjectTemplates", templateVersion!, source, true, ct);
                    });

            if (templateInstallExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  The template installation failed with exit code {templateInstallExitCode}. For more information run with --debug switch.[/]");
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

            int newProjectExitCode = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .SpinnerStyle(Style.Parse("purple"))
                .StartAsync(
                    ":rocket:  Creating new Aspire project...",
                    async context => {
                        return await cliRunner.NewProjectAsync(
                    templateName,
                    name,
                    outputPath,
                    ct);
                });

            if (newProjectExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down: Project creation failed with exit code {newProjectExitCode}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            return ExitCodeConstants.Success;
        });

        parentCommand.Subcommands.Add(command);
    }

    private static (string FriendlyName, NuGetPackage Package) GetPackageByInteractiveFlow(IEnumerable<(string FriendlyName, NuGetPackage Package)> knownPackages)
    {
        var packagePrompt = new SelectionPrompt<(string FriendlyName, NuGetPackage Package)>()
            .Title("Select an integration to add:")
            .UseConverter(PackageNameWithFriendlyNameIfAvailable)
            .PageSize(10)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"))
            .AddChoices(knownPackages);

        var selectedIntegration = AnsiConsole.Prompt(packagePrompt);

        var versionPrompt = new TextPrompt<string>($"Specify a version of {selectedIntegration.Package.Id}")
            .DefaultValue(selectedIntegration.Package.Version)
            .Validate(value => string.IsNullOrEmpty(value) ? ValidationResult.Error("Version cannot be empty.") : ValidationResult.Success())
            .ShowDefaultValue(true)
            .DefaultValueStyle(Style.Parse("darkmagenta"));

        var version = AnsiConsole.Prompt(versionPrompt);

        selectedIntegration.Package.Version = version;

        return selectedIntegration;

        static string PackageNameWithFriendlyNameIfAvailable((string FriendlyName, NuGetPackage Package) packageWithFriendlyName)
        {
            if (packageWithFriendlyName.FriendlyName is { } friendlyName)
            {
                return $"[bold]{friendlyName}[/] ({packageWithFriendlyName.Package.Id})";
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
        var command = new Command("add", "Add an integration or other resource to the Aspire project.");

        var resourceArgument = new Argument<string>("resource");
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var versionOption = new Option<string>("--version", "-v");
        command.Options.Add(versionOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        command.Options.Add(prereleaseOption);

        var sourceOption = new Option<string?>("--source", "-s");
        command.Options.Add(sourceOption);

        command.SetAction(async (parseResult, ct) => {

            try
            {
                var app = BuildApplication(parseResult);
                
                var integrationLookup = app.Services.GetRequiredService<INuGetPackageCache>();

                var integrationName = parseResult.GetValue<string>("resource");

                var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
                var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);
                
                if (effectiveAppHostProjectFile is null)
                {
                    return ExitCodeConstants.FailedToFindProject;
                }

                var prerelease = parseResult.GetValue<bool>("--prerelease");

                var source = parseResult.GetValue<string?>("--source");

                var packages = await AnsiConsole.Status().StartAsync(
                    "Searching for Aspire packages...",
                    context => integrationLookup.GetPackagesAsync(effectiveAppHostProjectFile, prerelease, source, ct)
                    );

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
                    "Adding Aspire integration...",
                    async context => {
                        var runner = app.Services.GetRequiredService<DotNetCliRunner>();
                        var addPackageResult = await runner.AddPackageAsync(
                            effectiveAppHostProjectFile,
                            selectedNuGetPackage.Package.Id,
                            selectedNuGetPackage.Package.Version,
                            ct
                            );

                        return addPackageResult == 0 ? ExitCodeConstants.Success : ExitCodeConstants.FailedToAddPackage;
                    }
                );

                return addPackageResult;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down: An error occurred while adding the package: {ex.Message}[/]");
                return ExitCodeConstants.FailedToAddPackage;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    public static Task<int> Main(string[] args)
    {
        System.Console.OutputEncoding = Encoding.UTF8;
        var rootCommand = GetRootCommand();
        var config = new CommandLineConfiguration(rootCommand);
        config.EnableDefaultExceptionHandler = true;
        return config.InvokeAsync(args);
    }
}
