// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Templating;

namespace Aspire.Cli.Commands;

/// <summary>
/// Is a universal type of command, that can be used to perform operation of different kinds against target resources.
///
/// <c>
/// Example: aspire exec --resource MyAppUsingEf --command 'dotnet ef migrations add Initial'
///          where target resource is app, which uses Entity Framework
///          and the command is creation of a migration source
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
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1823 // Avoid unused private fields

    public ExecCommand(
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
        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _interactionService = interactionService;

        var targetResourceOption = new Option<string>("--resource", "-r");
        targetResourceOption.Description = "The target resource to perform an operation against.";
        // targetResourceOption.Recursive = true; ?
        Options.Add(targetResourceOption);

        var commandOption = new Option<string?>("--command", "-c");
        commandOption.Description = "The command to perform against the target resource.";
        // commandOption.Recursive = true; ?
        Options.Add(commandOption);
    }

    protected override Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        throw new NotImplementedException();
    }
}
