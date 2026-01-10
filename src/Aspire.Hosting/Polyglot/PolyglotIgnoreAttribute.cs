// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Polyglot;

/// <summary>
/// Marks a method or type as excluded from polyglot code generation.
/// </summary>
/// <remarks>
/// Use this attribute to prevent specific methods, types, or properties from being included
/// in generated TypeScript/Python SDK bindings. This is useful for methods that:
/// <list type="bullet">
/// <item>Have unsupported parameter types or signatures</item>
/// <item>Are intended only for internal use</item>
/// <item>Would create confusing or unnecessary API surface in guest languages</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // This method will not appear in generated SDKs
/// [PolyglotIgnore]
/// public static IResourceBuilder&lt;T&gt; WithAdvancedConfiguration&lt;T&gt;(
///     this IResourceBuilder&lt;T&gt; builder,
///     Action&lt;IConfiguration, IServiceProvider&gt; configure);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class PolyglotIgnoreAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PolyglotIgnoreAttribute"/> class.
    /// </summary>
    public PolyglotIgnoreAttribute()
    {
        Languages = PolyglotLanguages.All;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolyglotIgnoreAttribute"/> class
    /// for specific languages only.
    /// </summary>
    /// <param name="languages">The languages for which this member should be ignored.</param>
    public PolyglotIgnoreAttribute(PolyglotLanguages languages)
    {
        Languages = languages;
    }

    /// <summary>
    /// Gets the languages for which this member should be ignored.
    /// </summary>
    public PolyglotLanguages Languages { get; }
}
