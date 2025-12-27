// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Interface for generating language-specific code from the Aspire application model.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Gets the target language name (e.g., "TypeScript", "Python").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Generates the distributed application SDK code.
    /// </summary>
    /// <param name="model">The application model to generate from.</param>
    /// <returns>A dictionary of file paths to file contents.</returns>
    Dictionary<string, string> GenerateDistributedApplication(ApplicationModel model);

    /// <summary>
    /// Generates code for a specific integration package.
    /// </summary>
    /// <param name="integration">The integration model.</param>
    /// <returns>A dictionary of file paths to file contents.</returns>
    Dictionary<string, string> GenerateIntegration(IntegrationModel integration);

    /// <summary>
    /// Generates code for a specific resource type.
    /// </summary>
    /// <param name="resource">The resource model.</param>
    /// <returns>A dictionary of file paths to file contents.</returns>
    Dictionary<string, string> GenerateResource(ResourceModel resource);
}
