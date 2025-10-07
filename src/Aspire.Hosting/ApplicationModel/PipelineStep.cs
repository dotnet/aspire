// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a step in a deployment pipeline.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineStep
{
    private readonly List<string> _resolvedDependencies = [];
    private readonly List<Func<Publishing.PipelineStepRegistry, IEnumerable<string>>> _dependencyResolvers = [];

    /// <summary>
    /// Gets or sets the name of the pipeline step.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of step names that this step depends on.
    /// This list is populated after dependency resolution.
    /// </summary>
    public IReadOnlyList<string> Dependencies => _resolvedDependencies.AsReadOnly();

    /// <summary>
    /// Gets or sets the action to execute for this step.
    /// The action receives a DeployingContext for deployment services and a PipelineContext for sharing data between steps.
    /// </summary>
    public Func<DeployingContext, PipelineContext, Task>? Action { get; set; }

    /// <summary>
    /// Adds a dependency resolver callback that will be evaluated during the dependency resolution phase.
    /// </summary>
    /// <param name="resolver">A function that takes a PipelineStepRegistry and returns step names to depend on.</param>
    /// <returns>The current pipeline step for method chaining.</returns>
    internal PipelineStep DependsOn(Func<Publishing.PipelineStepRegistry, IEnumerable<string>> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _dependencyResolvers.Add(resolver);
        return this;
    }

    /// <summary>
    /// Adds a dependency on a step by name.
    /// </summary>
    /// <param name="stepName">The name of the step to depend on.</param>
    /// <returns>The current pipeline step for method chaining.</returns>
    internal PipelineStep DependsOn(string stepName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        return DependsOn(_ => [stepName]);
    }

    /// <summary>
    /// Resolves all dependency callbacks using the provided registry.
    /// This is called during the second pass of pipeline setup.
    /// </summary>
    /// <param name="registry">The pipeline step registry containing all registered steps.</param>
    internal void ResolveDependencies(Publishing.PipelineStepRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _resolvedDependencies.Clear();

        foreach (var resolver in _dependencyResolvers)
        {
            var dependencies = resolver(registry);
            foreach (var dependency in dependencies)
            {
                if (!_resolvedDependencies.Contains(dependency, StringComparer.OrdinalIgnoreCase))
                {
                    // Validate that the dependency exists in the registry
                    if (!registry.HasStep(dependency))
                    {
                        var availableSteps = string.Join(", ", registry.GetAllStepNames());
                        throw new InvalidOperationException(
                            $"Step '{Name}' depends on '{dependency}' which does not exist in the pipeline. " +
                            $"Available steps: {availableSteps}");
                    }

                    _resolvedDependencies.Add(dependency);
                }
            }
        }
    }
}
