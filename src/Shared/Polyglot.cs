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
/// Specifies an alternative method name for a polyglot model in a given set of programming languages.
/// </summary>
/// <remarks>Apply this attribute to a method to indicate its corresponding name in other supported languages, as
/// defined by the polyglot model. This is useful for tools or frameworks that generate or map code across multiple
/// languages, ensuring consistent method naming conventions.</remarks>
[AttributeUsage(AttributeTargets.Method)]
public class PolyglotMethodNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the PolyglotMethodNameAttribute class with the specified method name and optional
    /// target languages.
    /// </summary>
    /// <param name="methodName">The name of the method as it should appear in the target language or languages. Cannot be null.</param>
    public PolyglotMethodNameAttribute(string methodName)
    {
        MethodName = methodName;
        Languages = PolyglotLanguages.All;
    }

    ///
    public PolyglotMethodNameAttribute(string methodName, PolyglotLanguages languages)
    {
        MethodName = methodName;
        Languages = languages;
    }

    /// <summary>
    /// Gets the set of languages supported by the polyglot model.
    /// </summary>
    public PolyglotLanguages Languages { get; }

    /// <summary>
    /// Gets the method name for the target language.
    /// </summary>
    public string MethodName { get; }
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
