// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Represents the type of AppHost project.
/// </summary>
internal enum AppHostType
{
    /// <summary>
    /// Traditional .NET project file (.csproj, .fsproj, .vbproj).
    /// </summary>
    DotNetProject,

    /// <summary>
    /// Single-file .NET AppHost (apphost.cs with #:sdk directive).
    /// </summary>
    DotNetSingleFile,

    /// <summary>
    /// TypeScript AppHost (apphost.ts or aspire.json with TypeScript).
    /// </summary>
    TypeScript,

    /// <summary>
    /// Python AppHost (apphost.py or aspire.json with Python).
    /// </summary>
    Python
}
