// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Spectre.Console;
using StreamJsonRpc;

namespace Aspire.Cli.Commands;

internal class TestCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IProjectLocator _projectLocator;
    private readonly AspireCliTelemetry _telemetry;

    public TestCommand(
        IDotNetCliRunner runner,
        IProjectLocator projectLocator,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext)
        : base("test", TestCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(features);

        _runner = runner;
        _projectLocator = projectLocator;
        _telemetry = telemetry;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = TestCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");

        var runOutputCollector = new OutputCollector();

        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

            var effectiveAppHostFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, createSettingsFile: false, cancellationToken);

            if (effectiveAppHostFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
            };

            var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var env = new Dictionary<string, string>();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostFile,
                watch: false,
                noBuild: false,
                Array.Empty<string>(),
                env,
                backchannelCompletitionSource,
                runOptions,
                cancellationToken);

            // Wait for the backchannel to be established.
            var backchannel = await InteractionService.ShowStatusAsync(TestCommandStrings.ConnectingToAppHost, async () => { return await backchannelCompletitionSource.Task.WaitAsync(cancellationToken); });

            InteractionService.DisplaySuccess(TestCommandStrings.AppHostStarted);

            // Connect to the auxiliary backchannel
            var auxiliaryBackchannel = await ConnectToAuxiliaryBackchannelAsync(effectiveAppHostFile.FullName, cancellationToken);

            // Wait for test results
            InteractionService.DisplayEmptyLine();
            var testResults = await InteractionService.ShowStatusAsync("Running tests...", async () =>
            {
                return await auxiliaryBackchannel.GetTestResultsAsync(cancellationToken);
            });

            // Display test results
            InteractionService.DisplayEmptyLine();
            if (testResults?.Success == true)
            {
                InteractionService.DisplaySuccess(testResults.Message);
            }
            else
            {
                InteractionService.DisplayError(testResults?.Message ?? "Test execution failed.");
            }

            // Stop the AppHost
            await auxiliaryBackchannel.StopAppHostAsync(cancellationToken);

            // Wait for the apphost to exit
            return await pendingRun;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService);
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message.EscapeMarkup()));
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message.EscapeMarkup()));
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    private static async Task<IAuxiliaryBackchannel> ConnectToAuxiliaryBackchannelAsync(string appHostPath, CancellationToken cancellationToken)
    {
        var socketPath = GetAuxiliaryBackchannelSocketPath(appHostPath);

        // Wait for the socket to be created (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (!File.Exists(socketPath))
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                throw new InvalidOperationException($"Auxiliary backchannel socket not found at {socketPath} after waiting {timeout.TotalSeconds} seconds");
            }

            await Task.Delay(100, cancellationToken);
        }

        // Give the socket a moment to be ready
        await Task.Delay(100, cancellationToken);

        // Connect to the Unix socket
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(socketPath);

        await socket.ConnectAsync(endpoint, cancellationToken);

        // Create JSON-RPC connection
        var stream = new NetworkStream(socket, ownsSocket: true);
        var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
        rpc.StartListening();

        return new AuxiliaryBackchannel(rpc);
    }

    private static string GetAuxiliaryBackchannelSocketPath(string appHostPath)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var backchannelsDir = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");

        // Compute hash from the AppHost path for consistency
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(appHostPath));
        // Use first 16 characters to keep socket path length reasonable
        var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();

        return Path.Combine(backchannelsDir, $"aux.sock.{hash}");
    }
}
