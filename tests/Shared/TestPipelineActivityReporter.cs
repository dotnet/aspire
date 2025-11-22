// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Utils;

/// <summary>
/// A test implementation of <see cref="IPipelineActivityReporter"/> that captures activity for test assertions.
/// </summary>
public sealed class TestPipelineActivityReporter : IPipelineActivityReporter
{
    private readonly ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestPipelineActivityReporter"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper to write to.</param>
    public TestPipelineActivityReporter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
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
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReportingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
    {
        CreatedSteps.Add(title);
        
        return Task.FromResult<IReportingStep>(new TestReportingStep(this, title, _testOutputHelper));
    }

    private sealed class TestReportingStep : IReportingStep
    {
        private readonly TestPipelineActivityReporter _reporter;
        private readonly string _title;
        private readonly ITestOutputHelper _testOutputHelper;

        public TestReportingStep(TestPipelineActivityReporter reporter, string title, ITestOutputHelper testOutputHelper)
        {
            _reporter = reporter;
            _title = title;
            _testOutputHelper = testOutputHelper;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            _reporter.CompletedSteps.Add((_title, completionText, completionState));
            
            return Task.CompletedTask;
        }

        public Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
        {
            _reporter.CreatedTasks.Add((_title, statusText));
            
            return Task.FromResult<IReportingTask>(new TestReportingTask(_reporter, statusText, _testOutputHelper));
        }

        public void Log(LogLevel logLevel, string message, bool enableMarkdown)
        {
            _reporter.LoggedMessages.Add((_title, logLevel, message));
        }
    }

    private sealed class TestReportingTask : IReportingTask
    {
        private readonly TestPipelineActivityReporter _reporter;
        private readonly string _initialStatusText;
        private readonly ITestOutputHelper _testOutputHelper;

        public TestReportingTask(TestPipelineActivityReporter reporter, string initialStatusText, ITestOutputHelper testOutputHelper)
        {
            _reporter = reporter;
            _initialStatusText = initialStatusText;
            _testOutputHelper = testOutputHelper;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            _reporter.CompletedTasks.Add((_initialStatusText, completionMessage, completionState));
            
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
        {
            _reporter.UpdatedTasks.Add((_initialStatusText, statusText));
            
            return Task.CompletedTask;
        }
    }
}
