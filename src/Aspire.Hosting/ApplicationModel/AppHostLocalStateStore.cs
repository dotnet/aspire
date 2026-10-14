// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

internal sealed class AppHostLocalStateStore
{
    // State updates only hold the lock for a small local JSON read/write. Persistence is best-effort,
    // so use a short timeout rather than allowing the shared FileLock default to delay AppHost startup.
    private static readonly TimeSpan s_lockTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<AppHostLocalStateStore> _logger;
    private readonly string? _stateDirectory;
    private readonly string? _statePath;
    private readonly string? _lockPath;

    public AppHostLocalStateStore(IConfiguration configuration, ILogger<AppHostLocalStateStore> logger)
    {
        _logger = logger;

        if (configuration["AppHost:Directory"] is not { Length: > 0 } appHostDirectory)
        {
            return;
        }

        var identity = configuration[KnownConfigNames.DcpWorkloadId]
            ?? configuration["AppHost:FilePath"]
            ?? configuration["AppHost:Path"]
            ?? Path.GetFullPath(appHostDirectory);
        var identityHash = Convert.ToHexString(XxHash3.Hash(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
        _stateDirectory = Path.Combine(appHostDirectory, ".aspire", "state");
        _statePath = Path.Combine(_stateDirectory, $"{identityHash}.json");
        _lockPath = _statePath + ".lock";
    }

    public bool IsAvailable => _statePath is not null;

    public T Read<T>(string key, Func<JsonNode?, T> readValue, T defaultValue)
    {
        if (_statePath is null || _lockPath is null)
        {
            return defaultValue;
        }

        try
        {
            using var fileLock = FileLock.Acquire(_lockPath, s_lockTimeout);
            var state = LoadState();
            return readValue(state[key]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
        {
            _logger.LogWarning(exception, "Failed to read AppHost local state from {StatePath}.", _statePath);
            return defaultValue;
        }
    }

    public void Update(string key, Func<JsonNode?, JsonNode?> updateValue)
    {
        if (_stateDirectory is null || _statePath is null || _lockPath is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_stateDirectory);
            EnsureStateDirectoryIsIgnored();

            using var fileLock = FileLock.Acquire(_lockPath, s_lockTimeout);
            var state = LoadState();
            var currentValue = state[key];
            var updatedValue = updateValue(currentValue?.DeepClone());
            if (JsonNode.DeepEquals(currentValue, updatedValue))
            {
                return;
            }

            if (updatedValue is null)
            {
                state.Remove(key);
            }
            else
            {
                state[key] = updatedValue;
            }

            WriteState(state);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
        {
            _logger.LogWarning(exception, "Failed to update AppHost local state in {StatePath}.", _statePath);
        }
    }

    private JsonObject LoadState()
    {
        if (!File.Exists(_statePath))
        {
            return [];
        }

        try
        {
            if (JsonNode.Parse(File.ReadAllText(_statePath)) is not JsonObject root)
            {
                _logger.LogWarning("Ignoring invalid AppHost local state file {StatePath}.", _statePath);
                return [];
            }

            return root;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ignoring malformed AppHost local state file {StatePath}.", _statePath);
            return [];
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Ignoring invalid AppHost local state file {StatePath}.", _statePath);
            return [];
        }
    }

    private void WriteState(JsonObject state)
    {
        var statePath = _statePath
            ?? throw new InvalidOperationException("AppHost local state path was not initialized.");
        var temporaryPath = statePath + $".{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryPath, state.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, statePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void EnsureStateDirectoryIsIgnored()
    {
        var ignorePath = Path.Combine(_stateDirectory!, ".gitignore");
        if (File.Exists(ignorePath))
        {
            return;
        }

        try
        {
            // State is derived from the AppHost model and must not require repository-specific ignore rules.
            using var stream = new FileStream(ignorePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            writer.WriteLine("*");
        }
        catch (IOException) when (File.Exists(ignorePath))
        {
            // Another AppHost process created the same product-owned ignore file.
        }
    }

}
