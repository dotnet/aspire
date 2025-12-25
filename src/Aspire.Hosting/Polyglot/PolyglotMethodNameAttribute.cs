// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Polyglot;

/// <summary>
/// Specifies an alternative method name to use when generating polyglot SDK bindings.
/// </summary>
/// <remarks>
/// Use this attribute to give method overloads unique names in generated TypeScript/Python SDKs,
/// avoiding naming conflicts that can occur when multiple overloads exist in C#.
/// </remarks>
/// <example>
/// <code>
/// // In C#, both methods are named "WithEnvironment"
/// public static IResourceBuilder&lt;T&gt; WithEnvironment&lt;T&gt;(this IResourceBuilder&lt;T&gt; builder, string name, string value);
///
/// [PolyglotMethodName("withEnvironmentCallback")]
/// public static IResourceBuilder&lt;T&gt; WithEnvironment&lt;T&gt;(this IResourceBuilder&lt;T&gt; builder, Action&lt;EnvironmentCallbackContext&gt; callback);
///
/// // In TypeScript, they become:
/// // redis.withEnvironment("KEY", "value")
/// // redis.withEnvironmentCallback((ctx) => { ... })
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PolyglotMethodNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PolyglotMethodNameAttribute"/> class
    /// with the specified method name for all supported languages.
    /// </summary>
    /// <param name="methodName">The name of the method as it should appear in generated SDKs.</param>
    public PolyglotMethodNameAttribute(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Languages = PolyglotLanguages.All;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolyglotMethodNameAttribute"/> class
    /// with the specified method name for specific languages.
    /// </summary>
    /// <param name="methodName">The name of the method as it should appear in generated SDKs.</param>
    /// <param name="languages">The languages for which this name should be used.</param>
    public PolyglotMethodNameAttribute(string methodName, PolyglotLanguages languages)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Languages = languages;
    }

    /// <summary>
    /// Gets the method name to use in generated SDKs.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the languages for which this method name should be used.
    /// </summary>
    public PolyglotLanguages Languages { get; }
}
