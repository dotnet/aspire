// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Execution;

/// <summary>
/// Parses command-line strings into executable names and arguments.
/// Uses a canonical grammar across all platforms and rejects shell operators.
/// </summary>
internal interface ICommandLineParser
{
    /// <summary>
    /// Parses a command-line string into the executable name and arguments.
    /// </summary>
    /// <param name="commandLine">The command-line string to parse.</param>
    /// <returns>A tuple containing the executable name and the list of arguments.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command line contains shell operators like |, &gt;, &lt;, &amp;&amp;, ||, ;, $(), or backticks.
    /// </exception>
    (string FileName, IReadOnlyList<string> Args) Parse(string commandLine);
}
