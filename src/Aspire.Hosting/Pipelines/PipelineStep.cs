// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents a step in the deployment pipeline.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineStep
{
    private PipelineStepStatus _status = PipelineStepStatus.Pending;
    private readonly object _statusLock = new object();

    /// <summary>
    /// Gets or initializes the unique name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the action to execute for this step.
    /// </summary>
    public required Func<PipelineStepContext, Task> Action { get; init; }

    /// <summary>
    /// Gets or initializes the list of step names that this step depends on.
    /// </summary>
    public List<string> DependsOnSteps { get; init; } = [];

    /// <summary>
    /// Gets or initializes the list of step names that require this step to complete before they can finish.
    /// </summary>
    public List<string> RequiredBySteps { get; init; } = [];

    /// <summary>
    /// Gets the current execution status of the step.
    /// </summary>
    public PipelineStepStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
    }

    /// <summary>
    /// Gets or initializes the list of tags that categorize this step.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Transitions the step to a new status, validating that the transition is valid.
    /// </summary>
    /// <param name="newStatus">The new status to transition to.</param>
    /// <returns>True if the transition was successful, false if the transition was invalid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an invalid state transition is attempted.</exception>
    internal bool TryTransitionStatus(PipelineStepStatus newStatus)
    {
        lock (_statusLock)
        {
            // Validate the state transition
            var isValid = IsValidTransition(_status, newStatus);
            if (!isValid)
            {
                return false;
            }

            _status = newStatus;
            return true;
        }
    }

    /// <summary>
    /// Determines if a transition from one status to another is valid.
    /// </summary>
    private static bool IsValidTransition(PipelineStepStatus current, PipelineStepStatus next)
    {
        // Allow any transition if we're already in a terminal state and trying to stay there
        if (current == next)
        {
            return true;
        }

        return (current, next) switch
        {
            // From Pending, can go to Running or Failed (if dependency fails)
            (PipelineStepStatus.Pending, PipelineStepStatus.Running) => true,
            (PipelineStepStatus.Pending, PipelineStepStatus.Failed) => true,

            // From Running, can go to any terminal state
            (PipelineStepStatus.Running, PipelineStepStatus.Succeeded) => true,
            (PipelineStepStatus.Running, PipelineStepStatus.Failed) => true,
            (PipelineStepStatus.Running, PipelineStepStatus.Canceled) => true,

            // Terminal states (Succeeded, Failed, Canceled) cannot transition to other states
            _ => false
        };
    }

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="stepName">The name of the step to depend on.</param>
    public void DependsOn(string stepName)
    {
        DependsOnSteps.Add(stepName);
    }

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="step">The step to depend on.</param>
    public void DependsOn(PipelineStep step)
    {
        DependsOnSteps.Add(step.Name);
    }

    /// <summary>
    /// Specifies that this step is required by another step.
    /// </summary>
    /// <param name="stepName">The name of the step that requires this step.</param>
    public void RequiredBy(string stepName)
    {
        RequiredBySteps.Add(stepName);
    }

    /// <summary>
    /// Specifies that this step is required by another step.
    /// </summary>
    /// <param name="step">The step that requires this step.</param>
    public void RequiredBy(PipelineStep step)
    {
        RequiredBySteps.Add(step.Name);
    }
}
