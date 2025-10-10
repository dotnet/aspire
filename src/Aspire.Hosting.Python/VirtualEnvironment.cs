// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Python;

/// <summary>
/// Provides utilities for locating executables and managing paths within a Python virtual environment.
/// </summary>
/// <remarks>
/// <para>
/// A Python virtual environment is an isolated Python installation that has its own set of installed packages,
/// separate from the system Python. This class handles the platform-specific directory structure differences
/// between Windows (Scripts directory) and Unix-like systems (bin directory).
/// </para>
/// <para>
/// This class is used internally by the Python hosting infrastructure to locate the correct Python executable
/// and other tools (like pip, uv) within a virtual environment.
/// </para>
/// </remarks>
/// <param name="virtualEnvironmentPath">
/// The absolute path to the root directory of the virtual environment.
/// This directory should contain the standard virtual environment structure with bin/ or Scripts/ subdirectories.
/// </param>
internal sealed class VirtualEnvironment(string virtualEnvironmentPath)
{
    /// <summary>
    /// Locates an executable in the virtual environment using platform-specific paths.
    /// </summary>
    /// <param name="name">
    /// The name of the executable without platform-specific extensions.
    /// For example, "python" will resolve to "python.exe" on Windows and "python" on Unix.
    /// </param>
    /// <returns>
    /// The full path to the executable within the virtual environment.
    /// On Windows, returns the path in the Scripts directory with .exe extension.
    /// On Unix-like systems, returns the path in the bin directory without extension.
    /// </returns>
    /// <remarks>
    /// This method does not verify that the executable actually exists at the returned path.
    /// It constructs the expected path based on the virtual environment structure.
    /// </remarks>
    public string GetExecutable(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(virtualEnvironmentPath, "Scripts", name + ".exe");
        }

        return Path.Join(virtualEnvironmentPath, "bin", name);
    }
}
