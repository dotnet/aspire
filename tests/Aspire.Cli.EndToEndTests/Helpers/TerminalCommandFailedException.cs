// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal.Automation;

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// Exception thrown when a terminal command fails (exits with non-zero status).
/// Includes a snapshot of the terminal state at the time of failure.
/// </summary>
public sealed class TerminalCommandFailedException : Exception
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="snapshot">The terminal snapshot at the time of failure.</param>
    /// <param name="commandSequence">The command sequence number that failed.</param>
    public TerminalCommandFailedException(string message, Hex1bTerminalSnapshot snapshot, int commandSequence)
        : base(message)
    {
        Snapshot = snapshot;
        CommandSequence = commandSequence;
        TerminalContent = snapshot.GetScreenText();
    }

    /// <summary>
    /// The terminal snapshot at the time of failure.
    /// </summary>
    public Hex1bTerminalSnapshot Snapshot { get; }

    /// <summary>
    /// The command sequence number that failed.
    /// </summary>
    public int CommandSequence { get; }

    /// <summary>
    /// The text content of the terminal at the time of failure.
    /// </summary>
    public string TerminalContent { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"""
            {GetType().FullName}: {Message}
            Command Sequence: {CommandSequence}
            
            Terminal Content:
            {TerminalContent}
            
            {StackTrace}
            """;
    }
}
