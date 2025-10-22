// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Provides contextual information for a specific pipeline step execution.
/// </summary>
/// <remarks>
/// This context combines the shared pipeline context with a step-specific publishing step,
/// allowing each step to track its own tasks and completion state independently.
/// </remarks>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PipelineStepContext
{
    /// <summary>
    /// Gets the pipeline context shared across all steps.
    /// </summary>
    public required PipelineContext PipelineContext { get; init; }

    /// <summary>
    /// Gets the publishing step associated with this specific step execution.
    /// </summary>
    /// <value>
    /// The <see cref="IReportingStep"/> instance that can be used to create tasks and manage the publishing process for this step.
    /// </value>
    public required IReportingStep ReportingStep { get; init; }

    /// <summary>
    /// Gets the distributed application model to be deployed.
    /// </summary>
    public DistributedApplicationModel Model => PipelineContext.Model;

    /// <summary>
    /// Gets the execution context for the distributed application.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext => PipelineContext.ExecutionContext;

    /// <summary>
    /// Gets the service provider for dependency resolution.
    /// </summary>
    public IServiceProvider Services => PipelineContext.Services;

    /// <summary>
    /// Gets the logger for pipeline operations that writes to both the pipeline logger and the step logger.
    /// </summary>
    public ILogger Logger => _logger ??= new StepLogger(PipelineContext.Logger, ReportingStep);
    private ILogger? _logger;

    /// <summary>
    /// Gets the cancellation token for the pipeline operation.
    /// </summary>
    public CancellationToken CancellationToken => PipelineContext.CancellationToken;

    /// <summary>
    /// Gets the output path for deployment artifacts.
    /// </summary>
    public string? OutputPath => PipelineContext.OutputPath;
}

/// <summary>
/// An ILogger implementation that writes to both the pipeline logger and the step logger.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class StepLogger : ILogger
{
    private readonly ILogger _pipelineLogger;
    private readonly IReportingStep _step;

    public StepLogger(ILogger pipelineLogger, IReportingStep step)
    {
        _pipelineLogger = pipelineLogger;
        _step = step;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _pipelineLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _pipelineLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Log to the pipeline logger first
        _pipelineLogger.Log(logLevel, eventId, state, exception, formatter);

        // Also log to the step logger (for publishing output display)
        var message = formatter(state, exception);
        if (exception != null)
        {
            message = $"{message} {exception}";
        }
        
        _step.Log(logLevel, message);
    }
}
