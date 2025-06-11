// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

/// <summary>
/// Is a universal type of command, that can be used to perform operation of different kinds against target resources.
/// <br/>
/// 
/// <c>--project</c> regulates which AppHost to use
/// <c>--resource</c> regulates which resource (csproj?) to perform the operation against
/// <c>--command</c> is the actual command to perform against the target resource
/// <br/>
/// 
/// Example: <c>
/// aspire exec
///   --project MyAppHost
///   --resource MyAppUsingEf
///   --command 'dotnet ef migrations add Initial'
/// </c> 
/// </summary>
internal class ExecCommand : BaseCommand
{
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Avoid unused private fields
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1823 // Avoid unused private fields

    public ExecCommand(
        IProjectLocator projectLocator,
        IDotNetCliRunner runner,
        INuGetPackageCache nuGetPackageCache,
        INewCommandPrompter prompter,
        IInteractionService interactionService,
        ICertificateService certificateService)
        : base("exec", "Perform operation against target resource.")
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _interactionService = interactionService;
        _projectLocator = projectLocator;

        var appHostProjectOption = new Option<FileInfo?>("--project");
        appHostProjectOption.Description = "The path to the project file to add the integration to.";
        Options.Add(appHostProjectOption);

        var targetResourceOption = new Option<string>("--resource", "-r");
        targetResourceOption.Description = "The target resource to perform an operation against.";
        // targetResourceOption.Recursive = true; ?
        Options.Add(targetResourceOption);

        var commandOption = new Option<string?>("--command", "-c");
        commandOption.Description = "The command to perform against the target resource.";
        // commandOption.Recursive = true; ?
        Options.Add(commandOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();
        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        // resource can be missing;
        // if missing it just executes in the context of the apphost

        var command = parseResult.GetValue<string?>("--command");
        if (string.IsNullOrEmpty(command))
        {
            // command is required
            return ExitCodeConstants.InvalidCommand;
        }

        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);
        if (effectiveAppHostProjectFile is null)
        {
            return ExitCodeConstants.FailedToFindProject;
        }

        // build stuff here. environment, flags, etc
        var env = new Dictionary<string, string>();
        var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();
        var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

        var runOptions = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = runOutputCollector.AppendOutput,
            StandardErrorCallback = runOutputCollector.AppendError,
        };

        var pendingRun = _runner.RunAsync(
            effectiveAppHostProjectFile,
            watch: false, // for now
            noBuild: true, // for now
            args: ["--operation", "exec", "--command", command, ..unmatchedTokens],
            env: env,
            backchannelCompletionSource: backchannelCompletionSource,
            options: runOptions,
            cancellationToken);

        return await pendingRun;
    }
}
