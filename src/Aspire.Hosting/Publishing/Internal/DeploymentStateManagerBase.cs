// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing.Internal;

/// <summary>
/// Base class for deployment state managers that provides common functionality for state management,
/// concurrency control, and section-based locking.
/// </summary>
/// <typeparam name="T">The type of the derived class for logger typing.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="DeploymentStateManagerBase{T}"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public abstract class DeploymentStateManagerBase<T>(ILogger<T> logger) : IDeploymentStateManager where T : class
{
    /// <summary>
    /// JSON serializer options used for writing deployment state files.
    /// </summary>
    protected static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Logger instance for the derived class.
    /// </summary>
    protected readonly ILogger<T> logger = logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sectionLocks = new();
    private readonly ConcurrentDictionary<string, long> _sectionVersions = new();
    private JsonObject? _state;
    private bool _isStateLoaded;

    /// <inheritdoc/>
    public abstract string? StateFilePath { get; }

    /// <summary>
    /// Gets the path where the state file should be stored. Returns null if the path cannot be determined.
    /// </summary>
    protected abstract string? GetStatePath();

    /// <summary>
    /// Saves the state to the appropriate storage location.
    /// </summary>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract Task SaveStateToStorageAsync(JsonObject state, CancellationToken cancellationToken);

    /// <summary>
    /// Flattens a JsonObject using colon-separated keys for configuration compatibility.
    /// Handles both nested objects and arrays with indexed keys.
    /// </summary>
    /// <param name="source">The source JsonObject to flatten.</param>
    /// <returns>A flattened JsonObject.</returns>
    public static JsonObject FlattenJsonObject(JsonObject source)
    {
        var result = new JsonObject();
        FlattenJsonObjectRecursive(source, string.Empty, result);
        return result;
    }

    /// <summary>
    /// Unflattens a JsonObject that uses colon-separated keys back into a nested structure.
    /// Handles both nested objects and arrays with indexed keys.
    /// </summary>
    /// <param name="source">The flattened JsonObject to unflatten.</param>
    /// <returns>An unflattened JsonObject with nested structure.</returns>
    public static JsonObject UnflattenJsonObject(JsonObject source)
    {
        var result = new JsonObject();

        foreach (var kvp in source)
        {
            var keys = kvp.Key.Split(':');
            var current = result;

            for (var i = 0; i < keys.Length - 1; i++)
            {
                var key = keys[i];
                if (!current.TryGetPropertyValue(key, out var existing) || existing is not JsonObject)
                {
                    var newObject = new JsonObject();
                    current[key] = newObject;
                    current = newObject;
                }
                else
                {
                    current = existing.AsObject();
                }
            }

            current[keys[^1]] = kvp.Value?.DeepClone();
        }

        return result;
    }

    private static void FlattenJsonObjectRecursive(JsonObject source, string prefix, JsonObject result)
    {
        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is JsonObject nestedObject)
            {
                FlattenJsonObjectRecursive(nestedObject, key, result);
            }
            else if (kvp.Value is JsonArray array)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    var arrayKey = $"{key}:{i}";
                    if (array[i] is JsonObject arrayObject)
                    {
                        FlattenJsonObjectRecursive(arrayObject, arrayKey, result);
                    }
                    else
                    {
                        result[arrayKey] = array[i]?.DeepClone();
                    }
                }
            }
            else
            {
                result[key] = kvp.Value?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Loads the deployment state from storage, using caching to avoid repeated loads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded state as a JsonObject.</returns>
    protected async Task<JsonObject> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isStateLoaded && _state is not null)
            {
                return _state;
            }

            var jsonDocumentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            var statePath = GetStatePath();

            if (statePath is not null && File.Exists(statePath))
            {
                var fileContent = await File.ReadAllTextAsync(statePath, cancellationToken).ConfigureAwait(false);
                var flattenedState = JsonNode.Parse(fileContent, documentOptions: jsonDocumentOptions)!.AsObject();
                _state = UnflattenJsonObject(flattenedState);
            }
            else
            {
                _state = [];
            }

            _isStateLoaded = true;
            return _state;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        await SaveStateToStorageAsync(state, cancellationToken).ConfigureAwait(false);
    }

    private SemaphoreSlim GetSectionLock(string sectionName)
    {
        return _sectionLocks.GetOrAdd(sectionName, _ => new SemaphoreSlim(1, 1));
    }

    /// <inheritdoc/>
    public async Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
    {
        await LoadStateAsync(cancellationToken).ConfigureAwait(false);

        var sectionLock = GetSectionLock(sectionName);
        await sectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var version = _sectionVersions.GetOrAdd(sectionName, 0);
            var sectionData = _state?.TryGetPropertyValue(sectionName, out var sectionNode) == true && sectionNode is JsonObject obj
                ? obj
                : null;

            return new DeploymentStateSection(sectionName, sectionData, version, () => sectionLock.Release());
        }
        catch
        {
            sectionLock.Release();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default)
    {
        await LoadStateAsync(cancellationToken).ConfigureAwait(false);

        if (_state is null)
        {
            throw new InvalidOperationException("State has not been loaded.");
        }

        var currentVersion = _sectionVersions.GetOrAdd(section.SectionName, 0);
        if (currentVersion != section.Version)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict detected in section '{section.SectionName}'. " +
                $"Expected version {section.Version}, but current version is {currentVersion}. " +
                $"This typically indicates the section was modified after it was acquired. " +
                $"Ensure the section is saved before disposing or being modified by another operation.");
        }

        _state[section.SectionName] = section.Data;
        _sectionVersions[section.SectionName] = section.Version + 1;

        await SaveStateToStorageAsync(_state, cancellationToken).ConfigureAwait(false);
    }
}
