// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.Python;

/// <summary>
/// Handles location of files within the virtual environment of a python app.
/// </summary>
/// <param name="virtualEnvironmentPath">The path to the directory containing the python app files.</param>
internal sealed class VirtualEnvironment(string virtualEnvironmentPath)
{
    /// <summary>
    /// Checks if the virtual environment exists.
    /// </summary>
    /// <returns>True if the virtual environment directory exists, false otherwise.</returns>
    public bool Exists() => Directory.Exists(virtualEnvironmentPath);

    /// <summary>
    /// Creates the virtual environment if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when the virtual environment creation fails.</exception>
    public async Task CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        if (Exists())
        {
            return;
        }

        var parentDirectory = Path.GetDirectoryName(virtualEnvironmentPath);
        if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-m venv \"{virtualEnvironmentPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new DistributedApplicationException("Failed to start python process to create virtual environment.");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            throw new DistributedApplicationException($"Failed to create virtual environment at '{virtualEnvironmentPath}'. Error: {error}");
        }
    }

    /// <summary>
    /// Locates an executable in the virtual environment if the virtual environment exists.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>Returns the path to the executable if it exists in the virtual environment, or null if the virtual environment doesn't exist or the executable is not found.</returns>
    public string? GetExecutableIfVenvExists(string name)
    {
        if (!Exists())
        {
            return null;
        }
        return GetExecutable(name);
    }

    /// <summary>
    /// Locates an executable in the virtual environment.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>Returns the path to the executable if it exists in the virtual environment.</returns>
    public string? GetExecutable(string name)
    {
        if(OperatingSystem.IsWindows())
        {
            string[] allowedExtensions = [".exe", ".cmd", ".bat"];

            foreach(var allowedExtension in allowedExtensions)
            {
                string executablePath = Path.Join(virtualEnvironmentPath, "Scripts", name + allowedExtension);

                if(File.Exists(executablePath))
                {
                    return executablePath;
                }
            }
        }
        else
        {
            var executablePath = Path.Join(virtualEnvironmentPath, "bin", name);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }
        }

        return null;
    }

    /// <summary>
    /// Locates a required executable in the virtual environment.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>The path to the executable in the virtual environment.</returns>
    /// <exception cref="DistributedApplicationException">Gets thrown when the executable couldn't be located.</exception>
    public string GetRequiredExecutable(string name)
    {
        return GetExecutable(name) ?? throw new DistributedApplicationException(
            $"The executable {name} could not be found in the virtual environment at '{virtualEnvironmentPath}' . " +
            "Make sure the virtual environment is initialized and the executable is installed.");
    }
}
