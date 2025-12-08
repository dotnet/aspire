// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Configuration;

/// <summary>
/// Service responsible for resolving the active channel based on CLI flags, environment variables, and settings files.
/// </summary>
internal interface IChannelResolver
{
    /// <summary>
    /// Resolves the active channel based on the precedence order:
    /// 1. CLI --channel flag
    /// 2. ASPIRE_CHANNEL environment variable
    /// 3. Workspace settings file
    /// 4. Global settings file
    /// 5. "stable" fallback
    /// </summary>
    /// <param name="cliChannelOption">The value from the --channel CLI flag (if provided).</param>
    /// <param name="includeWorkspaceContext">Whether to check workspace settings. Set to false for commands like 'update --self'.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved channel name.</returns>
    Task<string> ResolveChannelAsync(string? cliChannelOption = null, bool includeWorkspaceContext = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the global settings.
    /// </summary>
    Task<GlobalSettings> GetGlobalSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the global settings.
    /// </summary>
    Task SetGlobalSettingsAsync(GlobalSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the workspace settings if they exist.
    /// </summary>
    Task<WorkspaceSettings?> GetWorkspaceSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the workspace settings.
    /// </summary>
    Task SetWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default);
}

internal class ChannelResolver(
    CliExecutionContext executionContext,
    FileInfo globalSettingsFile,
    ILogger<ChannelResolver> logger) : IChannelResolver
{
    private const string DefaultChannel = "stable";
    private const string ChannelEnvironmentVariable = "ASPIRE_CHANNEL";

    public async Task<string> ResolveChannelAsync(string? cliChannelOption = null, bool includeWorkspaceContext = true, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Resolving channel. CLI option: {CliOption}, Include workspace: {IncludeWorkspace}", 
            cliChannelOption ?? "null", includeWorkspaceContext);

        // 1. Check CLI flag
        if (!string.IsNullOrWhiteSpace(cliChannelOption))
        {
            logger.LogDebug("Channel resolved from CLI flag: {Channel}", cliChannelOption);
            return cliChannelOption;
        }

        // 2. Check environment variable
        var envChannel = executionContext.GetEnvironmentVariable(ChannelEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envChannel))
        {
            logger.LogDebug("Channel resolved from environment variable {EnvVar}: {Channel}", 
                ChannelEnvironmentVariable, envChannel);
            return envChannel;
        }

        // 3. Check workspace settings (if context included)
        if (includeWorkspaceContext)
        {
            var workspaceSettings = await GetWorkspaceSettingsAsync(cancellationToken);
            if (workspaceSettings?.Channel is not null)
            {
                logger.LogDebug("Channel resolved from workspace settings: {Channel}", workspaceSettings.Channel);
                return workspaceSettings.Channel;
            }
        }

        // 4. Check global settings
        var globalSettings = await GetGlobalSettingsAsync(cancellationToken);
        if (globalSettings.DefaultChannel is not null)
        {
            logger.LogDebug("Channel resolved from global settings: {Channel}", globalSettings.DefaultChannel);
            return globalSettings.DefaultChannel;
        }

        // 5. Fallback to default
        logger.LogDebug("Channel resolved to default: {Channel}", DefaultChannel);
        return DefaultChannel;
    }

    public async Task<GlobalSettings> GetGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(globalSettingsFile.FullName))
        {
            logger.LogDebug("Global settings file does not exist: {Path}", globalSettingsFile.FullName);
            return new GlobalSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(globalSettingsFile.FullName, cancellationToken);
            var settings = JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.GlobalSettings);
            logger.LogDebug("Loaded global settings from {Path}", globalSettingsFile.FullName);
            return settings ?? new GlobalSettings();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read global settings from {Path}", globalSettingsFile.FullName);
            return new GlobalSettings();
        }
    }

    public async Task SetGlobalSettingsAsync(GlobalSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(globalSettingsFile.FullName);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogDebug("Created directory for global settings: {Directory}", directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.GlobalSettings);
        await File.WriteAllTextAsync(globalSettingsFile.FullName, json, cancellationToken);
        logger.LogDebug("Saved global settings to {Path}", globalSettingsFile.FullName);
    }

    public async Task<WorkspaceSettings?> GetWorkspaceSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settingsPath = FindWorkspaceSettingsFile();
        if (settingsPath is null || !File.Exists(settingsPath))
        {
            logger.LogDebug("Workspace settings file not found");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath, cancellationToken);
            var settings = JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.WorkspaceSettings);
            logger.LogDebug("Loaded workspace settings from {Path}", settingsPath);
            return settings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read workspace settings from {Path}", settingsPath);
            return null;
        }
    }

    public async Task SetWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var settingsPath = ConfigurationHelper.BuildPathToSettingsJsonFile(executionContext.WorkingDirectory.FullName);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(settingsPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogDebug("Created directory for workspace settings: {Directory}", directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.WorkspaceSettings);
        await File.WriteAllTextAsync(settingsPath, json, cancellationToken);
        logger.LogDebug("Saved workspace settings to {Path}", settingsPath);
    }

    private string? FindWorkspaceSettingsFile()
    {
        var searchDirectory = executionContext.WorkingDirectory;

        // Walk up the directory tree to find existing settings file
        while (searchDirectory is not null)
        {
            var settingsFilePath = ConfigurationHelper.BuildPathToSettingsJsonFile(searchDirectory.FullName);

            if (File.Exists(settingsFilePath))
            {
                return settingsFilePath;
            }

            searchDirectory = searchDirectory.Parent;
        }

        return null;
    }
}
