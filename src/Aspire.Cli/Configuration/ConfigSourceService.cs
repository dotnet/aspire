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
    ConfigSourceConfigurationProvider provider,
    ILogger<ConfigSourceService> logger)
{
    internal const string MicrosoftRepository = "https://msft.ghe.com/coreai/aspire-1p-init";
    private readonly ConcurrentDictionary<ConfigSource, ConfigSourceSnapshot> _snapshots = [];
    private readonly ConcurrentDictionary<ConfigSource, byte> _reported = [];

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
            || command is AgentInitCommand && parseResult.GetValue(AgentInitCommand.s_scopeOption) == AgentConfigurationScope.Global)
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
        if (command is AgentInitCommand && parseResult.GetValue(AgentInitCommand.s_workspaceRootOption) is { } workspaceRoot)
        {
            directory = new DirectoryInfo(Path.GetFullPath(workspaceRoot, executionContext.WorkingDirectory.FullName));
        }
        await CheckProjectAsync(directory, force: false, cancellationToken);
    }

    public async Task<ConfigSource?> SelectForCreationAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var existingConfig = Path.Combine(executionContext.WorkingDirectory.FullName, AspireConfigFile.FileName);
        if (parseResult.CommandResult.Command is InitCommand && File.Exists(existingConfig))
        {
            var projectSettings = await ReadProjectConfigurationAsync(existingConfig, cancellationToken);
            if (ReadBinding(projectSettings) is { } saved)
            {
                var savedSnapshot = await AcquireAsync(saved, force: false, cancellationToken);
                provider.UseConfiguration(savedSnapshot.Configuration, existingConfig, projectSettings);
                return saved;
            }
        }

        var repository = environment.GetEnvironmentVariable("ASPIRE_INIT_SOURCE");
        var explicitSource = repository is not null;
        if (!explicitSource)
        {
            var detection = await detector.IsInternalMicrosoftMachineAsync(cancellationToken);
            if (!detection.IsInternalMicrosoft || detection.Outcome != InternalMicrosoftDetectorOutcome.Detected)
            {
                if (parseResult.GetValue(s_useSourceOption) is true)
                {
                    throw new ConfigSourceException(ConfigSourceStrings.NoCandidate);
                }
                return null;
            }
            repository = MicrosoftRepository;
        }

        var source = new ConfigSource(repository!, environment.GetEnvironmentVariable("ASPIRE_INIT_CONFIG") ?? AspireConfigFile.FileName);
        source.Validate();
        interaction.DisplayMessage(KnownEmojis.Information, string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.Candidate,
            source.Repository, source.Path, explicitSource ? ConfigSourceStrings.EnvironmentOrigin : ConfigSourceStrings.EmployeeOrigin));

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

        var snapshot = await AcquireAsync(source, force: false, cancellationToken);
        provider.UseConfiguration(snapshot.Configuration, configPath: null, projectSettings: null);
        return source;
    }

    public static async Task BindAsync(ConfigSource source, DirectoryInfo projectDirectory, CancellationToken cancellationToken)
    {
        var path = Path.Combine(projectDirectory.FullName, AspireConfigFile.FileName);
        var configuration = File.Exists(path)
            ? JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken), documentOptions: ConfigurationHelper.ParseOptions)?.AsObject()
                ?? throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration)
            : new JsonObject();
        configuration["configSource"] = JsonSerializer.SerializeToNode(source, JsonSourceGenerationContext.Default.ConfigSource);
        await ConfigurationHelper.WriteSettingsFileAsync(path, configuration, cancellationToken);
    }

    public async Task CheckProjectAsync(DirectoryInfo directory, bool force, CancellationToken cancellationToken)
    {
        if (!force && executionContext.Command is UpdateCommand { IsConfigSourceRefresh: true })
        {
            // Project selection must not perform an automatic fetch immediately before
            // refresh-only mode makes its one unconditional fetch.
            return;
        }
        var configPath = ConfigurationHelper.FindNearestConfigFilePath(directory);
        var projectSettings = configPath is null ? null : await ReadProjectConfigurationAsync(configPath, cancellationToken);
        var source = ReadBinding(projectSettings);
        if (source is null)
        {
            if (force)
            {
                throw new ConfigSourceException(ConfigSourceStrings.NoBinding);
            }
            provider.ClearConfiguration();
            return;
        }
        var snapshot = await AcquireAsync(source, force, cancellationToken);
        provider.UseConfiguration(snapshot.Configuration, configPath, projectSettings);
    }

    internal async Task<ConfigSource?> ReadBindingAsync(string configPath, CancellationToken cancellationToken)
    {
        return ReadBinding(await ReadProjectConfigurationAsync(configPath, cancellationToken));
    }

    private async Task<JsonObject?> ReadProjectConfigurationAsync(string configPath, CancellationToken cancellationToken)
    {
        using var configLock = await ConfigurationHelper.TryAcquireWorkspaceConfigLockAsync(
            executionContext, new FileInfo(configPath), logger, cancellationToken);
        // Capture local overrides with the binding under the same lock as workspace-default
        // writers. Release it before Git acquisition; the invocation uses this one local snapshot.
        return File.Exists(configPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(configPath, cancellationToken), documentOptions: ConfigurationHelper.ParseOptions)?.AsObject()
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
        var key = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes($"{source.Repository}\n{source.Path}")).ToString("x16", CultureInfo.InvariantCulture);
        return Path.Combine(executionContext.AspireHomeDirectory.FullName, "configsources", key);
    }

    internal async Task<ConfigSourceSnapshot> AcquireAsync(ConfigSource source, bool force, CancellationToken cancellationToken)
    {
        source.Validate();
        if (!force && _snapshots.TryGetValue(source, out var existing))
        {
            return existing;
        }

        var cacheDirectory = GetCacheDirectory(source);
        CreatePrivateDirectory(Path.GetDirectoryName(cacheDirectory)!);
        CreatePrivateDirectory(cacheDirectory);
        using var cacheLock = await FileLock.AcquireAsync(Path.Combine(cacheDirectory, "cache.lock"), cancellationToken);
        var pointer = Path.Combine(cacheDirectory, "current");
        var previousDirectory = File.Exists(pointer) ? (await File.ReadAllTextAsync(pointer, cancellationToken)).Trim() : null;
        ConfigSourceSnapshot? previous = null;
        if (previousDirectory is not null)
        {
            if (!Guid.TryParseExact(previousDirectory, "N", out _))
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidCache);
            }
            var previousPath = Path.Combine(cacheDirectory, previousDirectory);
            var metadata = JsonSerializer.Deserialize(await File.ReadAllTextAsync(Path.Combine(previousPath, "metadata.json"), cancellationToken),
                JsonSourceGenerationContext.Default.ConfigSourceMetadata);
            var configuration = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(previousPath, AspireConfigFile.FileName), cancellationToken),
                documentOptions: ConfigurationHelper.ParseOptions) as JsonObject;
            if (metadata is null || metadata.Source != source || configuration is null)
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidCache);
            }
            previous = new ConfigSourceSnapshot(metadata, configuration);
        }

        var age = previous is null ? TimeSpan.MaxValue : timeProvider.GetUtcNow() - previous.Metadata.RefreshedAt;
        if (!force && previous is not null && age >= TimeSpan.Zero && age < TimeSpan.FromHours(24))
        {
            _snapshots[source] = previous;
            Report(source, previous, ConfigSourceStrings.Cached);
            return previous;
        }

        var snapshotName = Guid.NewGuid().ToString("N");
        var snapshotDirectory = Path.Combine(cacheDirectory, snapshotName);
        var temporaryPointer = Path.Combine(cacheDirectory, $"{snapshotName}.tmp");
        CreatePrivateDirectory(snapshotDirectory);
        var published = false;
        try
        {
            var (commit, configuration) = await git.AcquireAsync(source, snapshotDirectory, cancellationToken);
            var snapshot = new ConfigSourceSnapshot(new(source, commit, timeProvider.GetUtcNow()), configuration);
            await File.WriteAllTextAsync(Path.Combine(snapshotDirectory, AspireConfigFile.FileName), configuration.ToJsonString(), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(snapshotDirectory, "metadata.json"),
                JsonSerializer.Serialize(snapshot.Metadata, JsonSourceGenerationContext.Default.ConfigSourceMetadata), cancellationToken);
            await File.WriteAllTextAsync(temporaryPointer, snapshotName, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            // The immutable snapshot contains both files. Only this pointer is replaced, so
            // failures or cancellation cannot publish half a configuration/metadata pair.
            File.Move(temporaryPointer, pointer, overwrite: true);
            published = true;
            _snapshots[source] = snapshot;
            Report(source, snapshot, previous?.Configuration.ToJsonString() == configuration.ToJsonString()
                ? ConfigSourceStrings.RefreshedUnchanged : ConfigSourceStrings.Refreshed);
            return snapshot;
        }
        finally
        {
            File.Delete(temporaryPointer);
            if (!published)
            {
                try
                {
                    // Git object files can be read-only on Windows, even in a failed clone.
                    foreach (var file in Directory.EnumerateFiles(snapshotDirectory, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }
                    Directory.Delete(snapshotDirectory, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    interaction.DisplayMessage(KnownEmojis.Warning, ConfigSourceStrings.CleanupFailed, consoleOverride: ConsoleOutput.Error);
                }
            }
        }
    }

    private void Report(ConfigSource source, ConfigSourceSnapshot snapshot, string state)
    {
        if (_reported.TryAdd(source, 0))
        {
            interaction.DisplayMessage(KnownEmojis.Information, string.Format(CultureInfo.CurrentCulture, ConfigSourceStrings.UseNotice,
                source.Repository, source.Path, snapshot.Metadata.Commit, state), consoleOverride: ConsoleOutput.Error);
        }
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
}
