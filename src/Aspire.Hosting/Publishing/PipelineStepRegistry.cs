// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Registry for tracking and discovering pipeline steps during deployment.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class PipelineStepRegistry
{
    private readonly Dictionary<string, PipelineStep> _steps = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Registers a pipeline step in the registry.
    /// </summary>
    /// <param name="step">The pipeline step to register.</param>
    /// <returns>The registered step for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a step with the same name is already registered.</exception>
    public PipelineStep Register(PipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(step.Name, nameof(step.Name));

        lock (_lock)
        {
            if (_steps.ContainsKey(step.Name))
            {
                throw new InvalidOperationException($"A pipeline step with name '{step.Name}' has already been registered.");
            }

            _steps[step.Name] = step;
        }

        return step;
    }

    /// <summary>
    /// Tries to get a pipeline step by name.
    /// </summary>
    /// <param name="name">The name of the step.</param>
    /// <param name="step">The found step, or null if not found.</param>
    /// <returns>True if the step was found, false otherwise.</returns>
    public bool TryGetStep(string name, [NotNullWhen(true)] out PipelineStep? step)
    {
        lock (_lock)
        {
            return _steps.TryGetValue(name, out step);
        }
    }

    /// <summary>
    /// Gets a pipeline step by name.
    /// </summary>
    /// <param name="name">The name of the step.</param>
    /// <returns>The pipeline step.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the step is not found.</exception>
    public PipelineStep GetStep(string name)
    {
        if (!TryGetStep(name, out var step))
        {
            var availableSteps = string.Join(", ", GetAllStepNames());
            throw new InvalidOperationException(
                $"Pipeline step '{name}' not found. Available steps: {availableSteps}");
        }
        return step;
    }

    /// <summary>
    /// Gets all registered pipeline steps.
    /// </summary>
    /// <returns>A read-only collection of all steps.</returns>
    public IReadOnlyCollection<PipelineStep> GetAllSteps()
    {
        lock (_lock)
        {
            return _steps.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all registered step names.
    /// </summary>
    /// <returns>A collection of step names.</returns>
    public IReadOnlyCollection<string> GetAllStepNames()
    {
        lock (_lock)
        {
            return _steps.Keys.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if a step with the given name is registered.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if a step with the name exists, false otherwise.</returns>
    public bool HasStep(string name)
    {
        lock (_lock)
        {
            return _steps.ContainsKey(name);
        }
    }
}
