// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Context information for creating an AppHost runner.
/// </summary>
internal sealed class AppHostRunnerContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostRunnerContext"/> class.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to run.</param>
    /// <param name="settingsFile">The settings file associated with the AppHost, if it exists.</param>
    public AppHostRunnerContext(FileInfo appHostFile, FileInfo? settingsFile = null)
    {
        ArgumentNullException.ThrowIfNull(appHostFile);

        AppHostFile = appHostFile;
        SettingsFile = settingsFile;
    }

    /// <summary>
    /// Gets the AppHost file to run.
    /// </summary>
    public FileInfo AppHostFile { get; }

    /// <summary>
    /// Gets the settings file associated with the AppHost, if it exists.
    /// </summary>
    public FileInfo? SettingsFile { get; }
}
