// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a single line of output from a process.
/// </summary>
/// <param name="IsStdErr">True if this line came from stderr, false if from stdout.</param>
/// <param name="Text">The text content of the line.</param>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public readonly record struct OutputLine(bool IsStdErr, string Text);
