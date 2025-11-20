// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Utils;

/// <summary>
/// A test implementation of <see cref="IPipelineActivityReporter"/> that logs activity to an <see cref="ITestOutputHelper"/> or <see cref="ILogger"/>.
/// </summary>
public sealed class TestPipelineActivityReporter : IPipelineActivityReporter
{
    private readonly ITestOutputHelper? _testOutputHelper;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestPipelineActivityReporter"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper to log to.</param>
    public TestPipelineActivityReporter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestPipelineActivityReporter"/> class.
    /// </summary>
    /// <param name="logger">The logger to log to.</param>
    public TestPipelineActivityReporter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a list of all steps that have been created.
    /// </summary>
    public List<string> CreatedSteps { get; } = [];

    /// <summary>
    /// Gets a list of all tasks that have been created.
    /// </summary>
    public List<(string StepTitle, string TaskStatusText)> CreatedTasks { get; } = [];

    /// <summary>
    /// Gets a list of all steps that have been completed.
    /// </summary>
    public List<(string StepTitle, string CompletionText, CompletionState CompletionState)> CompletedSteps { get; } = [];

    /// <summary>
    /// Gets a list of all tasks that have been completed.
    /// </summary>
    public List<(string TaskStatusText, string? CompletionMessage, CompletionState CompletionState)> CompletedTasks { get; } = [];

    /// <summary>
    /// Gets a list of all tasks that have been updated.
    /// </summary>
    public List<(string TaskStatusText, string StatusText)> UpdatedTasks { get; } = [];

    /// <summary>
    /// Gets a list of all log messages that have been logged.
    /// </summary>
    public List<(string StepTitle, LogLevel LogLevel, string Message)> LoggedMessages { get; } = [];

    /// <summary>
    /// Gets a value indicating whether <see cref="CompletePublishAsync"/> has been called.
    /// </summary>
    public bool CompletePublishCalled { get; private set; }

    /// <summary>
    /// Gets the completion message passed to <see cref="CompletePublishAsync"/>.
    /// </summary>
    public string? CompletionMessage { get; private set; }

    /// <inheritdoc />
    public Task CompletePublishAsync(string? completionMessage = null, CompletionState? completionState = null, CancellationToken cancellationToken = default)
    {
        CompletePublishCalled = true;
        CompletionMessage = completionMessage;
        
        var message = $"[CompletePublish] {completionMessage} (State: {completionState})";
        _testOutputHelper?.WriteLine(message);
        _logger?.LogInformation(message);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReportingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
    {
        CreatedSteps.Add(title);
        
        var message = $"[CreateStep] {title}";
        _testOutputHelper?.WriteLine(message);
        _logger?.LogInformation(message);
        
        return Task.FromResult<IReportingStep>(new TestReportingStep(this, title, _testOutputHelper, _logger));
    }

    private sealed class TestReportingStep : IReportingStep
    {
        private readonly TestPipelineActivityReporter _reporter;
        private readonly string _title;
        private readonly ITestOutputHelper? _testOutputHelper;
        private readonly ILogger? _logger;

        public TestReportingStep(TestPipelineActivityReporter reporter, string title, ITestOutputHelper? testOutputHelper, ILogger? logger)
        {
            _reporter = reporter;
            _title = title;
            _testOutputHelper = testOutputHelper;
            _logger = logger;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            _reporter.CompletedSteps.Add((_title, completionText, completionState));
            
            var message = $"  [CompleteStep:{_title}] {completionText} (State: {completionState})";
            _testOutputHelper?.WriteLine(message);
            _logger?.LogInformation(message);
            
            return Task.CompletedTask;
        }

        public Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
        {
            _reporter.CreatedTasks.Add((_title, statusText));
            
            var message = $"    [CreateTask:{_title}] {statusText}";
            _testOutputHelper?.WriteLine(message);
            _logger?.LogInformation(message);
            
            return Task.FromResult<IReportingTask>(new TestReportingTask(_reporter, statusText, _testOutputHelper, _logger));
        }

        public void Log(LogLevel logLevel, string message, bool enableMarkdown)
        {
            _reporter.LoggedMessages.Add((_title, logLevel, message));
            
            var logMessage = $"    [{logLevel}:{_title}] {message}";
            _testOutputHelper?.WriteLine(logMessage);
            
            // Log using the appropriate log level
            switch (logLevel)
            {
                case LogLevel.Trace:
                    _logger?.LogTrace(logMessage);
                    break;
                case LogLevel.Debug:
                    _logger?.LogDebug(logMessage);
                    break;
                case LogLevel.Information:
                    _logger?.LogInformation(logMessage);
                    break;
                case LogLevel.Warning:
                    _logger?.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    _logger?.LogError(logMessage);
                    break;
                case LogLevel.Critical:
                    _logger?.LogCritical(logMessage);
                    break;
            }
        }
    }

    private sealed class TestReportingTask : IReportingTask
    {
        private readonly TestPipelineActivityReporter _reporter;
        private readonly string _initialStatusText;
        private readonly ITestOutputHelper? _testOutputHelper;
        private readonly ILogger? _logger;

        public TestReportingTask(TestPipelineActivityReporter reporter, string initialStatusText, ITestOutputHelper? testOutputHelper, ILogger? logger)
        {
            _reporter = reporter;
            _initialStatusText = initialStatusText;
            _testOutputHelper = testOutputHelper;
            _logger = logger;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            _reporter.CompletedTasks.Add((_initialStatusText, completionMessage, completionState));
            
            var message = $"      [CompleteTask:{_initialStatusText}] {completionMessage} (State: {completionState})";
            _testOutputHelper?.WriteLine(message);
            _logger?.LogInformation(message);
            
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
        {
            _reporter.UpdatedTasks.Add((_initialStatusText, statusText));
            
            var message = $"      [UpdateTask:{_initialStatusText}] {statusText}";
            _testOutputHelper?.WriteLine(message);
            _logger?.LogInformation(message);
            
            return Task.CompletedTask;
        }
    }
}
