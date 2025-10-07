// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Extension methods for working with pipeline steps and dependency resolution.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class PipelineStepExtensions
{
    /// <summary>
    /// Adds a dependency on another pipeline step by looking it up in the registry.
    /// </summary>
    /// <param name="step">The step to add the dependency to.</param>
    /// <param name="dependencyName">The name of the step to depend on.</param>
    /// <param name="required">Whether the dependency is required. If false, ignores missing dependencies.</param>
    /// <returns>The current step for fluent chaining.</returns>
    public static PipelineStep DependsOnStep(this PipelineStep step, string dependencyName, bool required = true)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyName);

        return step.DependsOn(registry =>
        {
            // If not required and step doesn't exist, return empty list
            if (!required && !registry.HasStep(dependencyName))
            {
                return [];
            }

            // For required dependencies, just return the name - ResolveDependencies will validate
            return [dependencyName];
        });
    }
}
