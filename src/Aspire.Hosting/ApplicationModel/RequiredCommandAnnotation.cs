// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which declares that a resource requires a specific command/executable to be available on the local machine PATH before it can start.
/// </summary>
/// <param name="command">The command string (file name or path) that should be validated.</param>
[DebuggerDisplay("Type = {GetType().Name,nq}, Command = {Command}")]
public class RequiredCommandAnnotation(string command) : IResourceAnnotation
{
    /// <summary>
    /// Gets the command string (file name or path) that should be validated.
    /// </summary>
    public string Command { get; } = command ?? throw new ArgumentNullException(nameof(command));

    /// <summary>
    /// Gets or sets an optional help link URL to guide users when the command is missing.
    /// </summary>
    public string? HelpLink { get; init; }

    /// <summary>
    /// Gets or sets an optional custom validation callback that will be invoked after the command has been resolved.
    /// </summary>
    /// <remarks>
    /// The callback receives the resolved full path to the command and a cancellation token.
    /// It should return a tuple indicating whether the command is valid and an optional validation message.
    /// </remarks>
    public Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? ValidationCallback { get; init; }
}
