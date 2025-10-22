// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Polyglot;

/// <summary>
/// Specifies that the decorated class or method should be ignored by polyglot processing for the specified languages.
/// </summary>
/// <remarks>Apply this attribute to classes or methods to exclude them from polyglot code generation, analysis,
/// or tooling for the indicated languages. This is useful when certain code elements are not relevant or compatible
/// with specific language targets. The attribute requires specifying the affected languages and a description
/// explaining the reason for exclusion.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PolyglotIgnoreAttribute() : Attribute
{
    /// <summary>
    /// Gets the set of languages supported by the polyglot model.
    /// </summary>
    public required PolyglotLanguages Languages { get; init; }

    /// <summary>
    /// Gets the reason associated with the object.
    /// </summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Specifies the set of supported programming languages for polyglot operations.
/// </summary>
/// <remarks>This enumeration supports bitwise combination of its member values. Use the Flags attribute to
/// represent multiple languages simultaneously. The value None indicates that no languages are specified.</remarks>
[Flags]
public enum PolyglotLanguages
{
    /// <summary>
    /// Indicates that no options or flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies the TypeScript language option.
    /// </summary>
    TypeScript = 1 << 1,

    /// <summary>
    /// Specifies the Python programming language option.
    /// </summary>
    Python = 1 << 2,

    /// <summary>
    /// Represents a combination of all supported language options, including TypeScript and Python.
    /// </summary>
    /// <remarks>Use this value to specify that operations should apply to all available languages rather than
    /// a single language option. This is typically used in scenarios where multi-language support is
    /// required.</remarks>
    All = TypeScript | Python
}
