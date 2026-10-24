// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Shared;
using Aspire.Shared.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigSourceService(
    CliExecutionContext executionContext,
    IEnvironment environment,
    IInternalMicrosoftDetector detector,
    IConfigSourceGitClient git,
    TimeProvider timeProvider,
    IInteractionService interaction,
    ICliHostEnvironment hostEnvironment,
    ILogger<ConfigSourceService> logger) : IDisposable
{
    internal const string MicrosoftRepository = "https://msft.ghe.com/coreai/aspire-1p-init";
    private static readonly string[][] s_allowedSettings = [["docs", "llmsTxtUrl"], ["docs", "api", "sitemapUrl"]];
    private ConfigSourceSnapshot? _snapshot;
    private string? _projectDirectory;
    private bool _projectChecked;
    private string? _notification;
    private readonly Lock _checksLock = new();
    private readonly CancellationTokenSource _stopping = new();
    private Task? _check;

    internal static readonly Option<bool?> s_useSourceOption = new("--use-config-source")
    {
        Description = ConfigSourceStrings.UseSourceOptionDescription,
        Recursive = true,
        Arity = ArgumentArity.ExactlyOne
    };

    /// <summary>
    /// Checks the resolved project's saved configuration source for updates without applying changes.
    /// Runs in the background and defers advisory messages until command exit.
    /// </summary>
    /// <param name="projectDirectory">The directory of the project already resolved by the caller.</param>
    /// <param name="cancellationToken">A token to cancel the source update check.</param>
    /// <exception cref="InvalidOperationException">The service has already been used for a different project.</exception>
    public void CheckForConfigSourceUpdates(DirectoryInfo projectDirectory, CancellationToken cancellationToken)
    {
        lock (_checksLock)
        {
            EnsureSameProject(projectDirectory);
            if (_check is not null || _stopping.IsCancellationRequested || executionContext.Command is UpdateCommand { IsConfigSourceRefresh: true })
            {
                return;
            }

            _check = Task.Run(async () =>
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopping.Token);
                try
                {
                    await CheckProjectAsync(projectDirectory, force: false, cancellation.Token);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    logger.LogTrace("Configuration source check cancelled because the CLI is shutting down.");
                }
                catch (Exception ex)
                {
                    // Advisory work must never fail the command. Log only the type: source
                    // failures can contain private repository URLs or credentials.
                    logger.LogDebug("Non-fatal configuration source check failure ({FailureType}).", ex.GetType().Name);
                }
            }, CancellationToken.None);
        }
    }

    public async Task StopChecksAsync()
    {
        lock (_checksLock)
        {
            _stopping.Cancel();
        }

        // Cancel and join, rather than abandoning Git or credential-helper child processes.
        await WaitForChecksAsync();
    }

    internal Task WaitForChecksAsync()
    {
        lock (_checksLock)
        {
            return _check ?? Task.CompletedTask;
        }
    }

    public void NotifyIfUpdateAvailable()
    {
        string? message;
        lock (_checksLock)
        {
            message = _notification;
            _notification = null;
        }
        if (message is not null)
        {
            interaction.DisplayMessage(KnownEmojis.Warning, message, consoleOverride: ConsoleOutput.Error);
        }
    }

    public void Dispose()
    {
        StopChecksAsync().GetAwaiter().GetResult();
        _stopping.Dispose();
    }

    /// <summary>
    /// Resolves a candidate configuration source from the environment or employee detection,
    /// obtains consent, and acquires the accepted source.
    /// </summary>
    public async Task<ConfigSource?> ResolveAndAcquireConfigSourceAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var repository = environment.GetEnvironmentVariable(CliConfigNames.InitSourceUrl);
        var explicitSource = repository is not null;
        if (!explicitSource)
        {
            var detection = await detector.IsInternalMicrosoftMachineAsync(cancellationToken);
            if (!detection.IsInternalMicrosoft || detection.Outcome != InternalMicrosoftDetectorOutcome.Detected)
            {
                if (parseResult.GetValue(s_useSourceOption) is true)
                {
                    throw new ConfigSourceException(string.Format(CultureInfo.CurrentCulture,
                        ConfigSourceStrings.NoCandidate, CliConfigNames.InitSourceUrl));
                }
                return null;
            }
            repository = MicrosoftRepository;
        }

        var source = new ConfigSource
        {
            Repository = repository!,
            Path = explicitSource
                ? environment.GetEnvironmentVariable(CliConfigNames.InitSourcePath) ?? AspireConfigFile.FileName
                : AspireConfigFile.FileName,
            Ref = explicitSource ? environment.GetEnvironmentVariable(CliConfigNames.InitSourceRef) : null
        };
        source.Validate();
        interaction.DisplayMessage(KnownEmojis.Information, string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.Candidate,
            source.Repository, source.Path, source.Ref ?? ConfigSourceStrings.DefaultBranch,
            explicitSource ? CliConfigNames.InitSourceUrl : ConfigSourceStrings.EmployeeOrigin));

        var accepted = parseResult.GetValue(s_useSourceOption);
        if (accepted is null)
        {
            if (!hostEnvironment.SupportsInteractiveInput)
            {
                throw new ConfigSourceException(ConfigSourceStrings.ConsentRequired);
            }
            // Boolean choices let the prompt select default(bool), highlighting decline
            // even when accept is first. String choices preserve the first-item default.
            var selection = await interaction.PromptForSelectionAsync(ConfigSourceStrings.UseSourcePrompt,
                new[] { ConfigSourceStrings.Accept, ConfigSourceStrings.Decline }, choice => choice,
                cancellationToken: cancellationToken);
            accepted = selection == ConfigSourceStrings.Accept;
        }
        if (!accepted.Value)
        {
            return null;
        }

        await AcquireAsync(source, force: false, cancellationToken);
        return source;
    }

    public async Task<ConfigSource?> SelectForInitializationAsync(
        DirectoryInfo projectDirectory, ParseResult parseResult, CancellationToken cancellationToken)
    {
        EnsureSameProject(projectDirectory);
        var path = Path.Combine(projectDirectory.FullName, AspireConfigFile.FileName);
        var configuration = await ReadProjectConfigurationAsync(path, cancellationToken);
        if (configuration is not null && TryGetValue(configuration, ["configSource"], out _))
        {
            CheckForConfigSourceUpdates(projectDirectory, cancellationToken);
            return null;
        }

        return await ResolveAndAcquireConfigSourceAsync(parseResult, cancellationToken);
    }

    public async Task MaterializeAsync(ConfigSource source, DirectoryInfo projectDirectory, CancellationToken cancellationToken)
    {
        EnsureSameProject(projectDirectory);
        var snapshot = await AcquireAsync(source, force: false, cancellationToken);
        await ApplySnapshotAsync(source, snapshot, projectDirectory, cancellationToken);
    }

    private async Task ApplySnapshotAsync(ConfigSource source, ConfigSourceSnapshot snapshot, DirectoryInfo projectDirectory, CancellationToken cancellationToken)
    {
        var path = Path.Combine(projectDirectory.FullName, AspireConfigFile.FileName);
        using var configLock = await ConfigurationHelper.TryAcquireWorkspaceConfigLockAsync(
            executionContext, new FileInfo(path), logger, cancellationToken)
            ?? throw new ConfigSourceException(ConfigSourceStrings.ConfigLockFailed);
        // Re-read after fetching and under the workspace-writer lock, so intervening
        // changes to unrelated settings survive this read/merge/write operation.
        var configuration = await ReadConfigurationFileAsync(path, cancellationToken) ?? new JsonObject();
        if (ReadBinding(configuration) is { } current && !ConfigSource.HasSameIdentity(current, source))
        {
            throw new ConfigSourceException(ConfigSourceStrings.BindingChanged);
        }
        var previous = configuration.ToJsonString();
        ConfigurationHelper.NormalizeSettings(configuration);
        foreach (var setting in s_allowedSettings)
        {
            if (TryGetValue(snapshot.Configuration, setting, out var value))
            {
                SetValue(configuration, setting, value);
            }
        }
        var binding = new ConfigSource
        {
            Repository = source.Repository,
            Path = source.Path,
            Ref = source.Ref,
            AppliedVersion = snapshot.Metadata.Version
        };
        configuration[GetKey(configuration, "configSource")] = JsonSerializer.SerializeToNode(binding, JsonSourceGenerationContext.Default.ConfigSource);
        var changed = previous != configuration.ToJsonString();
        if (changed)
        {
            await ConfigurationHelper.WriteSettingsFileAtomicallyAsync(path, configuration, cancellationToken);
        }
        lock (_checksLock)
        {
            _projectChecked = true;
        }
        interaction.DisplayMessage(KnownEmojis.Information, string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.UseNotice,
            source.Repository, source.Path, snapshot.Metadata.Commit,
            changed ? ConfigSourceStrings.Refreshed : ConfigSourceStrings.RefreshedUnchanged), consoleOverride: ConsoleOutput.Error);
    }

    public async Task CheckProjectAsync(DirectoryInfo directory, bool force, CancellationToken cancellationToken)
    {
        EnsureSameProject(directory);
        if (!force && executionContext.Command is UpdateCommand { IsConfigSourceRefresh: true })
        {
            // Project selection must not perform an automatic fetch immediately before
            // refresh-only mode makes its one unconditional fetch.
            return;
        }
        try
        {
            lock (_checksLock)
            {
                if (!force && _projectChecked)
                {
                    return;
                }
                _projectChecked = true;
            }
            var configPath = ConfigurationHelper.FindNearestConfigFilePath(directory);
            if (configPath is null)
            {
                if (force)
                {
                    throw new ConfigSourceException(ConfigSourceStrings.NoBinding);
                }
                return;
            }
            var source = await ReadBindingAsync(configPath, cancellationToken);
            if (source is null)
            {
                if (force)
                {
                    throw new ConfigSourceException(ConfigSourceStrings.NoBinding);
                }
                return;
            }
            if (force)
            {
                var snapshot = await AcquireAsync(source, force: true, cancellationToken);
                await ApplySnapshotAsync(source, snapshot, new DirectoryInfo(Path.GetDirectoryName(configPath)!), cancellationToken);
            }
            else if (string.IsNullOrEmpty(source.AppliedVersion))
            {
                ReportOnce(ConfigSourceStrings.AppliedVersionMissing);
            }
            else
            {
                var observed = await CheckVersionAsync(source, cancellationToken);
                if (observed.Version != source.AppliedVersion)
                {
                    ReportOnce(ConfigSourceStrings.UpdateAvailable);
                }
            }
        }
        catch (Exception ex) when (!force && ex is ConfigSourceException or JsonException or IOException or UnauthorizedAccessException or TimeoutException)
        {
            ReportCheckFailure(ex);
        }
    }

    private void ReportCheckFailure(Exception exception)
    {
        logger.LogDebug("Configuration source version check failed ({FailureType}).", exception.GetType().Name);
        var message = string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.CheckFailed,
            exception is ConfigSourceException ? exception.Message : ConfigSourceStrings.InvalidCache);
        ReportOnce(message);
    }

    internal async Task<ConfigSource?> ReadBindingAsync(string configPath, CancellationToken cancellationToken)
    {
        return ReadBinding(await ReadProjectConfigurationAsync(configPath, cancellationToken));
    }

    private async Task<JsonObject?> ReadProjectConfigurationAsync(string configPath, CancellationToken cancellationToken)
    {
        using var configLock = await ConfigurationHelper.TryAcquireWorkspaceConfigLockAsync(
            executionContext, new FileInfo(configPath), logger, cancellationToken);
        return await ReadConfigurationFileAsync(configPath, cancellationToken);
    }

    private static async Task<JsonObject?> ReadConfigurationFileAsync(string path, CancellationToken cancellationToken)
    {
        return File.Exists(path)
            ? JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken),
                documentOptions: ConfigurationHelper.ParseOptions) as JsonObject
                ?? throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration)
            : null;
    }

    private static ConfigSource? ReadBinding(JsonObject? config)
    {
        if (config is null)
        {
            return null;
        }
        var sourceProperty = config.FirstOrDefault(p => p.Key.Equals("configSource", StringComparison.OrdinalIgnoreCase));
        if (sourceProperty.Key is null)
        {
            return null;
        }
        try
        {
            var source = sourceProperty.Value?.Deserialize(JsonSourceGenerationContext.Default.ConfigSource)
                ?? throw new ConfigSourceException(ConfigSourceStrings.InvalidBinding);
            if (source.Repository is null || source.Path is null)
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidBinding);
            }
            source.Validate();
            return source;
        }
        catch (JsonException)
        {
            throw new ConfigSourceException(ConfigSourceStrings.InvalidBinding);
        }
    }

    internal string GetCacheDirectory(ConfigSource source)
    {
        var key = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes($"{source.Repository}\n{source.Path}\n{source.Ref ?? ""}")).ToString("x16", CultureInfo.InvariantCulture);
        return Path.Combine(executionContext.AspireHomeDirectory.FullName, "configsources", key);
    }

    internal async Task<ConfigSourceSnapshot> AcquireAsync(ConfigSource source, bool force, CancellationToken cancellationToken)
    {
        source.Validate();
        lock (_checksLock)
        {
            if (_snapshot is { } existing)
            {
                if (!ConfigSource.HasSameIdentity(existing.Metadata.Source, source))
                {
                    throw new InvalidOperationException("A configuration source service instance cannot be used for multiple configuration sources.");
                }
                if (!force)
                {
                    return existing;
                }
            }
        }

        var cacheDirectory = GetCacheDirectory(source);
        DirectoryHelper.CreateWithOwnerOnlyPermissions(Path.GetDirectoryName(cacheDirectory)!);
        DirectoryHelper.CreateWithOwnerOnlyPermissions(cacheDirectory);
        using var cacheLock = await FileLock.AcquireAsync(Path.Combine(cacheDirectory, "cache.lock"), cancellationToken);
        var pointer = Path.Combine(cacheDirectory, "current");
        var snapshotName = Guid.NewGuid().ToString("N");
        var snapshotDirectory = Path.Combine(cacheDirectory, snapshotName);
        var temporaryPointer = Path.Combine(cacheDirectory, $"{snapshotName}.tmp");
        DirectoryHelper.CreateWithOwnerOnlyPermissions(snapshotDirectory);
        var published = false;
        try
        {
            var identity = new ConfigSource
            {
                Repository = source.Repository,
                Path = source.Path,
                Ref = source.Ref
            };
            var (commit, version, configuration) = await git.AcquireAsync(identity, snapshotDirectory, cancellationToken);
            ConfigurationHelper.NormalizeSettings(configuration);
            var now = timeProvider.GetUtcNow();
            var snapshot = new ConfigSourceSnapshot(new(identity, commit, version, now), configuration);
            await File.WriteAllTextAsync(Path.Combine(snapshotDirectory, AspireConfigFile.FileName), configuration.ToJsonString(), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(snapshotDirectory, "metadata.json"),
                JsonSerializer.Serialize(snapshot.Metadata, JsonSourceGenerationContext.Default.ConfigSourceMetadata), cancellationToken);
            await File.WriteAllTextAsync(temporaryPointer, snapshotName, cancellationToken);
            await WriteCheckMetadataAsync(cacheDirectory, new(identity, commit, version, now, source.AppliedVersion), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            // The immutable snapshot contains both files. Only this pointer is replaced, so
            // failures or cancellation cannot publish half a configuration/metadata pair.
            File.Move(temporaryPointer, pointer, overwrite: true);
            published = true;
            lock (_checksLock)
            {
                _snapshot = snapshot;
            }
            return snapshot;
        }
        finally
        {
            File.Delete(temporaryPointer);
            if (!published)
            {
                DeleteAcquisitionDirectory(snapshotDirectory);
            }
        }
    }

    private async Task<ConfigSourceCheckMetadata> CheckVersionAsync(ConfigSource source, CancellationToken cancellationToken)
    {
        var cacheDirectory = GetCacheDirectory(source);
        DirectoryHelper.CreateWithOwnerOnlyPermissions(Path.GetDirectoryName(cacheDirectory)!);
        DirectoryHelper.CreateWithOwnerOnlyPermissions(cacheDirectory);
        using var cacheLock = await FileLock.AcquireAsync(Path.Combine(cacheDirectory, "cache.lock"), cancellationToken);
        var path = Path.Combine(cacheDirectory, "check.json");
        if (File.Exists(path))
        {
            var previous = JsonSerializer.Deserialize(await File.ReadAllTextAsync(path, cancellationToken),
                JsonSourceGenerationContext.Default.ConfigSourceCheckMetadata);
            if (previous is null || previous.Source is null || !ConfigSource.HasSameIdentity(previous.Source, source)
                || string.IsNullOrEmpty(previous.Commit) || string.IsNullOrEmpty(previous.Version))
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidCache);
            }
            var age = timeProvider.GetUtcNow() - previous.CheckedAt;
            // A different app can have applied a version newer than this shared observation.
            // Reuse a mismatch only when that applied version was actually checked against it.
            if (age >= TimeSpan.Zero && age < TimeSpan.FromHours(24)
                && (previous.Version == source.AppliedVersion || previous.ComparedAppliedVersion == source.AppliedVersion))
            {
                return previous;
            }
        }
        var temporaryDirectory = Directory.CreateTempSubdirectory("aspire-config-source-check-");
        try
        {
            var identity = new ConfigSource
            {
                Repository = source.Repository,
                Path = source.Path,
                Ref = source.Ref
            };
            var (commit, version) = await git.CheckAsync(identity, temporaryDirectory.FullName, cancellationToken);
            var metadata = new ConfigSourceCheckMetadata(identity, commit, version, timeProvider.GetUtcNow(), source.AppliedVersion);
            await WriteCheckMetadataAsync(cacheDirectory, metadata, cancellationToken);
            return metadata;
        }
        finally
        {
            DeleteAcquisitionDirectory(temporaryDirectory.FullName);
        }
    }

    private static Task WriteCheckMetadataAsync(string directory, ConfigSourceCheckMetadata metadata, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToNode(metadata, JsonSourceGenerationContext.Default.ConfigSourceCheckMetadata)!.AsObject();
        return ConfigurationHelper.WriteSettingsFileAtomicallyAsync(Path.Combine(directory, "check.json"), json, cancellationToken);
    }

    private void ReportOnce(string message)
    {
        lock (_checksLock)
        {
            _notification ??= message;
        }
    }

    private void EnsureSameProject(DirectoryInfo directory)
    {
        var path = Path.TrimEndingDirectorySeparator(directory.FullName);
        lock (_checksLock)
        {
            if (_projectDirectory is not null && !StringComparers.FileSystemPath.Equals(_projectDirectory, path))
            {
                throw new InvalidOperationException("A configuration source service instance cannot be used for multiple projects.");
            }
            _projectDirectory = path;
        }
    }

    private void DeleteAcquisitionDirectory(string directory)
    {
        try
        {
            // Git object files can be read-only on Windows, even after a failed fetch.
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
            }
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug("Configuration source directory cleanup failed ({FailureType}).", ex.GetType().Name);
        }
    }

    private static string GetKey(JsonObject configuration, string key)
        => configuration.Select(p => p.Key).FirstOrDefault(p => p.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? key;

    private static bool TryGetValue(JsonObject configuration, string[] path, out JsonNode? value)
    {
        JsonNode? current = configuration;
        foreach (var segment in path)
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(GetKey(obj, segment), out current))
            {
                value = null;
                return false;
            }
        }
        value = current;
        return true;
    }

    private static void SetValue(JsonObject configuration, string[] path, JsonNode? value)
    {
        var current = configuration;
        foreach (var segment in path[..^1])
        {
            var key = GetKey(current, segment);
            if (current[key] is not JsonObject nested)
            {
                nested = new JsonObject();
                current[key] = nested;
            }
            current = nested;
        }
        current[GetKey(current, path[^1])] = value?.DeepClone();
    }

}
