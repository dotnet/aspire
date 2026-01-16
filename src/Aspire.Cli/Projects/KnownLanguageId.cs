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
    /// </summary>
    public const string TypeScript = "typescript";

    /// <summary>
    /// The language ID for Python AppHost projects.
    /// </summary>
    public const string Python = "python";

    /// <summary>
    /// The display name for Python AppHost projects.
    /// </summary>
    public const string PythonDisplayName = "Python";
}
