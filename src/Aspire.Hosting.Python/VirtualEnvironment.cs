// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Python;

/// <summary>
/// Handles location of files within the virtual environment of a python app.
/// </summary>
/// <param name="virtualEnvironmentPath">The path to the directory containing the python app files.</param>
internal sealed class VirtualEnvironment(string virtualEnvironmentPath)
{
    /// <summary>
    /// The path to the virtual environment.
    /// </summary>
    public string VirtualEnvironmentPath => virtualEnvironmentPath;

    /// <summary>
    /// Locates an executable in the virtual environment.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>Returns the path to the executable if it exists in the virtual environment.</returns>
    public string GetExecutable(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(virtualEnvironmentPath, "Scripts", name + ".exe");
        }

        return Path.Join(virtualEnvironmentPath, "bin", name);
    }
}
