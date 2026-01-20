// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Known language identifiers for AppHost projects.
/// </summary>
internal static class KnownLanguageId
{
    /// <summary>
    /// The language ID for C# (.NET) AppHost projects.
    /// </summary>
    public const string CSharp = "csharp";

    /// <summary>
    /// The display name for C# (.NET) AppHost projects.
    /// </summary>
    public const string CSharpDisplayName = "C# (.NET)";

    /// <summary>
    /// The language ID for TypeScript (Node.js) AppHost projects.
    /// Format: {language}/{runtime} to support multiple runtimes.
    /// </summary>
    public const string TypeScript = "typescript/nodejs";

    /// <summary>
    /// Short alias for TypeScript that can be used on the command line.
    /// </summary>
    public const string TypeScriptAlias = "typescript";
}
