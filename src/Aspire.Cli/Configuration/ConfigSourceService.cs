// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Hashing;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
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
    ILogger<ConfigSourceService> logger)
{
    internal const string MicrosoftRepository = "https://msft.ghe.com/coreai/aspire-1p-init";
    private static readonly string[][] s_allowedSettings = [["docs", "llmsTxtUrl"], ["docs", "api", "sitemapUrl"]];
    private readonly ConcurrentDictionary<ConfigSourceKey, ConfigSourceSnapshot> _snapshots = [];
    private readonly ConcurrentDictionary<ConfigSourceKey, byte> _reported = [];
    private readonly ConcurrentDictionary<string, byte> _checkedProjects = new(StringComparers.FileSystemPath);

    internal static readonly Option<bool?> s_useSourceOption = new("--use-config-source")
    {
        Description = ConfigSourceStrings.UseSourceOptionDescription,
        Recursive = true,
        Arity = ArgumentArity.ExactlyOne
    };

    public async Task CheckInvocationAsync(BaseCommand command, ParseResult parseResult, CancellationToken cancellationToken)
    {
        // These operations do not select a project. In particular, global agent setup
        // must never require access to an organization's private project configuration.
        if (command is NewCommand or InitCommand or TemplateCommand or PsCommand
            or LsCommand or DoctorCommand or SetupCommand or CompletionsCommand
            or DashboardRunCommand or CacheCommand or AgentTelemetryCommand
            or CertificatesCleanCommand or CertificatesTrustCommand
            or TrayStartCommand or TrayStopCommand
            || command.Options.OfType<Option<bool>>().Any(option =>
                option.Name == "--global" && parseResult.GetValue(option))
            || command is StopCommand && command.Options.OfType<Option<bool>>().Any(option =>
                option.Name == "--all" && parseResult.GetValue(option))
            || command is StopCommand && command.Options.Any(option => option.Name == "--pid" && parseResult.GetResult(option) is not null)
            || command is UpdateCommand && command.Options.OfType<Option<bool>>().Any(option =>
                option.Name == "--self" && parseResult.GetValue(option))
            || command is UpdateCommand && parseResult.GetValue(UpdateCommand.s_configSourceOption)
            || command is AgentInitCommand &&
                (parseResult.GetResult(AgentInitCommand.s_scopeOption) is not { Implicit: false }
                    || parseResult.GetValue(AgentInitCommand.s_scopeOption) is not AgentConfigurationScope.Project))
        {
            return;
        }

        var directory = executionContext.WorkingDirectory;
        var appHostOption = command.Options.OfType<Option<FileInfo?>>().FirstOrDefault(option => option.Name is "--apphost" or "--project");
        if (appHostOption is not null && parseResult.GetValue(appHostOption) is { } appHost)
        {
            directory = Directory.Exists(appHost.FullName) ? new DirectoryInfo(appHost.FullName) : appHost.Directory!;
        }
        else if (appHostOption is not null
            && ConfigurationHelper.FindNearestConfigFilePath(directory) is { } configPath
            && Path.GetFileName(configPath) == AspireConfigFile.FileName)
        {
            try
            {
                var projectSettings = await ReadProjectConfigurationAsync(configPath, cancellationToken);
                if (projectSettings?.Deserialize(JsonSourceGenerationContext.Default.AspireConfigFile)?.AppHost?.Path is { Length: > 0 } configuredPath)
                {
                    var configuredFile = new FileInfo(Path.GetFullPath(configuredPath, Path.GetDirectoryName(configPath)!));
                    if (configuredFile.Exists)
                    {
                        directory = configuredFile.Directory!;
                    }
                }
            }
            catch (Exception ex) when (ex is ConfigSourceException or JsonException or IOException or UnauthorizedAccessException)
            {
                ReportCheckFailure(null, ex);
                return;
            }
        }
        if (command is AgentInitCommand && parseResult.GetValue(AgentInitCommand.s_workspaceRootOption) is { } workspaceRoot)
        {
            directory = new DirectoryInfo(Path.GetFullPath(workspaceRoot, executionContext.WorkingDirectory.FullName));
        }
        await CheckProjectAsync(directory, force: false, cancellationToken);
    }

    public async Task<ConfigSource?> SelectForCreationAsync(ParseResult parseResult, CancellationToken cancellationToken)
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
            accepted = await interaction.PromptForSelectionAsync(ConfigSourceStrings.UseSourcePrompt,
                new[] { true, false }, choice => choice ? ConfigSourceStrings.Accept : ConfigSourceStrings.Decline,
                cancellationToken: cancellationToken);
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
        var path = Path.Combine(projectDirectory.FullName, AspireConfigFile.FileName);
        var configuration = await ReadProjectConfigurationAsync(path, cancellationToken);
        if (configuration is not null && TryGetValue(configuration, ["configSource"], out _))
        {
            await CheckProjectAsync(projectDirectory, force: false, cancellationToken);
            return null;
        }

        return await SelectForCreationAsync(parseResult, cancellationToken);
    }

    public async Task MaterializeAsync(ConfigSource source, DirectoryInfo projectDirectory, CancellationToken cancellationToken)
    {
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
        _checkedProjects.TryAdd(path, 0);
        interaction.DisplayMessage(KnownEmojis.Information, string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.UseNotice,
            source.Repository, source.Path, snapshot.Metadata.Commit,
            changed ? ConfigSourceStrings.Refreshed : ConfigSourceStrings.RefreshedUnchanged), consoleOverride: ConsoleOutput.Error);
    }

    public async Task CheckProjectAsync(DirectoryInfo directory, bool force, CancellationToken cancellationToken)
    {
        if (!force && executionContext.Command is UpdateCommand { IsConfigSourceRefresh: true })
        {
            // Project selection must not perform an automatic fetch immediately before
            // refresh-only mode makes its one unconditional fetch.
            return;
        }
        ConfigSource? source = null;
        try
        {
            var configPath = ConfigurationHelper.FindNearestConfigFilePath(directory);
            if (configPath is null || (!force && !_checkedProjects.TryAdd(configPath, 0)))
            {
                if (force)
                {
                    throw new ConfigSourceException(ConfigSourceStrings.NoBinding);
                }
                return;
            }
            source = await ReadBindingAsync(configPath, cancellationToken);
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
                ReportOnce(source, ConfigSourceStrings.AppliedVersionMissing);
            }
            else
            {
                var observed = await CheckVersionAsync(source, cancellationToken);
                if (observed.Version != source.AppliedVersion)
                {
                    ReportOnce(source, ConfigSourceStrings.UpdateAvailable);
                }
            }
        }
        catch (Exception ex) when (!force && ex is ConfigSourceException or JsonException or IOException or UnauthorizedAccessException or TimeoutException)
        {
            ReportCheckFailure(source, ex);
        }
    }

    private void ReportCheckFailure(ConfigSource? source, Exception exception)
    {
        logger.LogDebug("Configuration source version check failed ({FailureType}).", exception.GetType().Name);
        var message = string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.CheckFailed,
            exception is ConfigSourceException ? exception.Message : ConfigSourceStrings.InvalidCache);
        if (source is null)
        {
            interaction.DisplayMessage(KnownEmojis.Warning, message, consoleOverride: ConsoleOutput.Error);
        }
        else
        {
            ReportOnce(source, message);
        }
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
        var key = new ConfigSourceKey(source.Repository, source.Path, source.Ref);
        if (!force && _snapshots.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var cacheDirectory = GetCacheDirectory(source);
        CreatePrivateDirectory(Path.GetDirectoryName(cacheDirectory)!);
        CreatePrivateDirectory(cacheDirectory);
        using var cacheLock = await FileLock.AcquireAsync(Path.Combine(cacheDirectory, "cache.lock"), cancellationToken);
        var pointer = Path.Combine(cacheDirectory, "current");
        var snapshotName = Guid.NewGuid().ToString("N");
        var snapshotDirectory = Path.Combine(cacheDirectory, snapshotName);
        var temporaryPointer = Path.Combine(cacheDirectory, $"{snapshotName}.tmp");
        CreatePrivateDirectory(snapshotDirectory);
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
            _snapshots[key] = snapshot;
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
        CreatePrivateDirectory(Path.GetDirectoryName(cacheDirectory)!);
        CreatePrivateDirectory(cacheDirectory);
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

    private void ReportOnce(ConfigSource source, string message)
    {
        if (_reported.TryAdd(new ConfigSourceKey(source.Repository, source.Path, source.Ref), 0))
        {
            interaction.DisplayMessage(KnownEmojis.Warning, message, consoleOverride: ConsoleOutput.Error);
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
            interaction.DisplayMessage(KnownEmojis.Warning, ConfigSourceStrings.CleanupFailed, consoleOverride: ConsoleOutput.Error);
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

    private static void CreatePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity();
            var user = WindowsIdentity.GetCurrent().User!;
            security.SetOwner(user);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            var directory = new DirectoryInfo(path);
            directory.Create(security);
            directory.SetAccessControl(security);
        }
        else
        {
            Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private readonly record struct ConfigSourceKey(string Repository, string Path, string? Ref);
}
