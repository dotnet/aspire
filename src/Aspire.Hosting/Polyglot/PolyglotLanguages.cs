// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Polyglot;

/// <summary>
/// Specifies the set of supported programming languages for polyglot operations.
/// </summary>
/// <remarks>
/// This enumeration supports bitwise combination of its member values. Use the Flags attribute to
/// represent multiple languages simultaneously.
/// </remarks>
[Flags]
public enum PolyglotLanguages
{
    /// <summary>
    /// Indicates that no languages are specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates TypeScript/JavaScript language support.
    /// </summary>
    TypeScript = 1 << 0,

    /// <summary>
    /// Indicates Python language support.
    /// </summary>
    Python = 1 << 1,

    /// <summary>
    /// Indicates all supported languages.
    /// </summary>
    All = TypeScript | Python
}
