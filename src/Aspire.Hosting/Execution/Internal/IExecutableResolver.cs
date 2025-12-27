// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Execution;

/// <summary>
/// Resolves executable names to full paths using PATH and PATHEXT (on Windows).
/// </summary>
internal interface IExecutableResolver
{
    /// <summary>
    /// Resolves an executable name to its full path.
    /// </summary>
    /// <param name="fileName">The executable name or path to resolve.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <returns>The full path to the executable.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the executable cannot be found in the PATH or working directory.
    /// </exception>
    string ResolveOrThrow(string fileName, ShellState state);
}
