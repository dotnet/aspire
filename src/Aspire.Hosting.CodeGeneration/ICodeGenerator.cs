// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Interface for generating language-specific code from ATS capabilities.
/// </summary>
internal interface ICodeGenerator
{
    /// <summary>
    /// Gets the target language name (e.g., "TypeScript", "Python").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Generates the distributed application SDK code from capabilities.
    /// </summary>
    /// <param name="capabilities">The capabilities to generate from.</param>
    /// <param name="dtoTypes">The DTO types to generate interfaces for.</param>
    /// <returns>A dictionary of file paths to file contents.</returns>
    Dictionary<string, string> GenerateDistributedApplication(
        IReadOnlyList<AtsCapabilityInfo> capabilities,
        IReadOnlyList<AtsDtoTypeInfo> dtoTypes);
}
