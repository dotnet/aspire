// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for creating AppHost runners.
/// </summary>
internal sealed class AppHostRunnerFactory : IAppHostRunnerFactory
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IConfiguration _configuration;
    private readonly IFeatures _features;
    private readonly CliExecutionContext _executionContext;

    public AppHostRunnerFactory(
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry,
        IConfiguration configuration,
        IFeatures features,
        CliExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(executionContext);

        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _ansiConsole = ansiConsole;
        _telemetry = telemetry;
        _configuration = configuration;
        _features = features;
        _executionContext = executionContext;
    }

    public IAppHostRunner CreateRunner(FileInfo appHostFile)
    {
        ArgumentNullException.ThrowIfNull(appHostFile);

        // For now, we only have one type of runner - the legacy .NET-based runner
        // In the future, we can add logic here to determine the appropriate runner
        // based on the file type or other characteristics
        return new LegacyAppHostRunner(
            appHostFile,
            _runner,
            _interactionService,
            _certificateService,
            _ansiConsole,
            _telemetry,
            _configuration,
            _features,
            _executionContext);
    }
}
