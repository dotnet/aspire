// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Projects;
using Aspire.Cli.Rendering;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal class RunExperimentalCommand : BaseCommand
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly IProjectLocator _projectLocator;
    private readonly IDotNetCliRunner _runner;
    private readonly ICertificateService _certificateService;

    public RunExperimentalCommand(IAnsiConsole ansiConsole, IProjectLocator projectLocator, IDotNetCliRunner runner, ICertificateService certificateService) : base("runx", "Experimental run command")
    {
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(runner);

        _ansiConsole = ansiConsole;
        _projectLocator = projectLocator;
        _runner = runner;
        _certificateService = certificateService;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        Options.Add(projectOption);
    }

    private async Task<int> RunAppHostAsync(ConsoleDashboardState state, FileInfo projectFile, CancellationToken cancellationToken)
    {
        var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

        var pendingRun = _runner.RunAsync(
            projectFile,
            false,
            false,
            Array.Empty<string>(),
            null,
            backchannelCompletitionSource,
            new DotNetCliRunnerInvocationOptions(),
            cancellationToken
            );

        await state.UpdateStatusAsync("Starting app host...", cancellationToken);

        var backchannel = await  backchannelCompletitionSource.Task;

        await state.UpdateStatusAsync("Backchannel connected.", cancellationToken);

        var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);

        await foreach (var resourceState in resourceStates.WithCancellation(cancellationToken))
        {
            await state.UpdateResourceAsync(resourceState, cancellationToken);
        }   

        return await pendingRun;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        Action finalSteps = () =>
        {
            _ansiConsole.Write(new ControlCode("\u001b[?1049l"));
        };
        int exitCode = 1;

        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);
        await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

        var state = new ConsoleDashboardState();
        _ = Task.Run(() => RunAppHostAsync(state, projectFile!, cancellationToken), cancellationToken);

        try
        {
            _ansiConsole.Write(new ControlCode("\u001b[?1049h\u001b[H"));

            var renderable = new ConsoleDashboardRenderable(state);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var keyInfo = Console.ReadKey(true);
                        await renderable.ProcessInputAsync(keyInfo.Key, cancellationToken);
                    }
                    catch (Exception)
                    {
                        Console.Beep();
                    }
                }
            }, cancellationToken);

            await _ansiConsole.Live(renderable).StartAsync(async context =>
            {
                await renderable.FocusAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    renderable.MakeDirty();
                    context.Refresh();

                    await state.Updated.Reader.ReadAsync(cancellationToken);
                }
            });

            exitCode = 0;
        }
        catch (OperationCanceledException)
        {
            finalSteps += () =>
            {
                _ansiConsole.WriteLine("Operation cancelled.");
            };
            // Operation was cancelled, exit gracefully.
            exitCode = 0;
        }
        catch (Exception ex)
        {
            finalSteps += () =>
            {
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                _ansiConsole.WriteException(ex, ExceptionFormats.Default);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            };
        }
        finally
        {
            finalSteps.Invoke();
        }

        return exitCode;
    }
}