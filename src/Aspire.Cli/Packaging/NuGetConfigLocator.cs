// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Packaging;

/// <summary>
/// Interface for locating NuGet.config files in directory hierarchies.
/// </summary>
public interface INuGetConfigLocator
{
    /// <summary>
    /// Searches for a NuGet.config file starting from the specified directory and moving up through parent directories.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <returns>A <see cref="FileInfo"/> representing the NuGet.config file if found; otherwise, null.</returns>
    FileInfo? FindNuGetConfig(DirectoryInfo startDirectory);

    /// <summary>
    /// Searches for a NuGet.config file starting from the current working directory and moving up through parent directories.
    /// </summary>
    /// <returns>A <see cref="FileInfo"/> representing the NuGet.config file if found; otherwise, null.</returns>
    FileInfo? FindNuGetConfig();
}

/// <summary>
/// Locates NuGet.config files by searching up through directory hierarchies.
/// </summary>
internal sealed class NuGetConfigLocator(Aspire.Cli.CliExecutionContext executionContext) : INuGetConfigLocator
{
    /// <inheritdoc />
    public FileInfo? FindNuGetConfig()
    {
        return FindNuGetConfig(executionContext.WorkingDirectory);
    }

    /// <inheritdoc />
    public FileInfo? FindNuGetConfig(DirectoryInfo startDirectory)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);

        var currentDirectory = startDirectory;

        while (currentDirectory is not null)
        {
            var configFile = currentDirectory.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(f.Name, "nuget.config", StringComparison.OrdinalIgnoreCase));
            
            if (configFile is not null)
            {
                return configFile;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
