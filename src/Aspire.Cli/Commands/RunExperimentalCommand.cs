// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Projects;
using Aspire.Cli.Rendering;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal class RunExperimentalCommand : BaseCommand
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly IProjectLocator _projectLocator;
    private readonly IDotNetCliRunner _runner;

    public RunExperimentalCommand(IAnsiConsole ansiConsole, IProjectLocator projectLocator, IDotNetCliRunner runner) : base("runx", "Experimental run command")
    {
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(runner);

        _ansiConsole = ansiConsole;
        _projectLocator = projectLocator;
        _runner = runner;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        Options.Add(projectOption);
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

        var state = new RunExperimentalState();

        try
        {
            _ansiConsole.Write(new ControlCode("\u001b[?1049h\u001b[H"));

            var renderable = new RunExperimentalRenderable(state);

            _ = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var keyInfo = Console.ReadKey(true);
                        renderable.ProcessInput(keyInfo.Key);
                    }
                    catch (Exception)
                    {
                        Console.Beep();
                    }
                }
            }, cancellationToken);

            _ = Task.Run(async () =>
            {
                await Task.Delay(2000, cancellationToken);
                await state.UpdateStatusAsync("Building app host.", cancellationToken);
                await Task.Delay(2000, cancellationToken);
                await state.UpdateStatusAsync("Launching app host.", cancellationToken);
                await Task.Delay(2000, cancellationToken);
                await state.UpdateStatusAsync("Starting dashboard", cancellationToken);

                var counter = 0;
                while (true)
                {
                    await Task.Delay(3000, cancellationToken);
                    await state.UpdateStatusAsync($"Counter: {counter}", cancellationToken);
                    counter++;
                }
            }, cancellationToken);

            await _ansiConsole.Live(renderable).StartAsync(async context =>
            {
                renderable.Focus();

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