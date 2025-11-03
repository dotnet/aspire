// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
/// <para>
/// Initializes a new instance of the <see cref="DeploymentStateManagerBase{T}"/> class.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// This class is thread-safe and designed for concurrent access. It uses the following synchronization mechanisms:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>_stateLock</c> (SemaphoreSlim): Protects access to the state file during load and save operations,
/// ensuring that file I/O operations are serialized and preventing concurrent modifications during enumeration.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>_sectionsLock</c> (object): Protects access to the <c>_sections</c> dictionary for version tracking,
/// ensuring atomic version checks and updates during section save operations.
/// </description>
/// </item>
/// </list>
/// The combination of these locks enables:
/// <list type="number">
/// <item>Safe concurrent reads of different sections</item>
/// <item>Optimistic concurrency control through version tracking</item>
/// <item>Serialized file writes to prevent corruption</item>
/// <item>Detection of concurrent modifications via version conflicts</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="logger">The logger instance.</param>
public abstract class DeploymentStateManagerBase<T>(ILogger<T> logger) : IDeploymentStateManager where T : class
{
    /// <summary>
    /// Holds section metadata including version information.
    /// </summary>
    private sealed class SectionMetadata(long version)
    {
        public long Version { get; } = version;
    }

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

    /// <summary>
    /// Semaphore protecting state file I/O operations. Ensures serialized access to file reads and writes.
    /// </summary>
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    /// <summary>
    /// Lock protecting access to the _sections dictionary for thread-safe version tracking.
    /// </summary>
    private readonly object _sectionsLock = new();

    /// <summary>
    /// Dictionary tracking version metadata for each section, protected by _sectionsLock.
    /// </summary>
    private readonly Dictionary<string, SectionMetadata> _sections = new();

    /// <summary>
    /// Cached state loaded from storage, protected by _stateLock during modification.
    /// </summary>
    private JsonObject? _state;

    /// <summary>
    /// Flag indicating whether state has been loaded from storage, accessed under _stateLock.
    /// </summary>
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
    /// <remarks>
    /// <strong>Thread Safety:</strong> This method uses _stateLock to ensure only one thread loads state from disk at a time.
    /// Subsequent calls return the cached state without re-reading the file.
    /// </remarks>
    protected async Task<JsonObject> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            _stateLock.Release();
        }
    }

    private SectionMetadata GetSectionMetadata(string sectionName)
    {
        lock (_sectionsLock)
        {
            if (!_sections.TryGetValue(sectionName, out var metadata))
            {
                metadata = new SectionMetadata(0);
                _sections[sectionName] = metadata;
            }
            return metadata;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <strong>Thread Safety:</strong> This method is thread-safe. It uses _stateLock to protect access to the state
    /// during section data retrieval, ensuring no concurrent modifications occur during the read operation.
    /// The returned section is a deep copy, making it safe to modify without affecting the stored state.
    /// </remarks>
    public async Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
    {
        await LoadStateAsync(cancellationToken).ConfigureAwait(false);

        var metadata = GetSectionMetadata(sectionName);

        // Protect access to _state with _stateLock to prevent concurrent modification during enumeration
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var sectionData = _state?.TryGetPropertyValue(sectionName, out var sectionNode) == true && sectionNode is JsonObject obj
                ? obj.DeepClone().AsObject()
                : null;

            return new DeploymentStateSection(sectionName, sectionData, metadata.Version);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <strong>Thread Safety:</strong> This method is thread-safe and uses a two-phase locking strategy:
    /// <list type="number">
    /// <item>
    /// <description>
    /// First, it acquires _sectionsLock to atomically check and update the version number,
    /// preventing concurrent modifications to the same section from succeeding.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Then, it acquires _stateLock to serialize the file write operation,
    /// ensuring no corruption occurs from concurrent saves.
    /// </description>
    /// </item>
    /// </list>
    /// If a version conflict is detected, an <see cref="InvalidOperationException"/> is thrown,
    /// indicating that the section was modified by another operation since it was acquired.
    /// </remarks>
    public async Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default)
    {
        await LoadStateAsync(cancellationToken).ConfigureAwait(false);

        if (_state is null)
        {
            throw new InvalidOperationException("State has not been loaded.");
        }

        // Atomically check version and update using lock + Dictionary
        lock (_sectionsLock)
        {
            if (_sections.TryGetValue(section.SectionName, out var metadata))
            {
                if (metadata.Version != section.Version)
                {
                    throw new InvalidOperationException(
                        $"Concurrency conflict detected in section '{section.SectionName}'. " +
                        $"Expected version {section.Version}, but current version is {metadata.Version}. " +
                        $"This typically indicates the section was modified after it was acquired. " +
                        $"Ensure the section is saved before being modified by another operation.");
                }
            }

            // Create new metadata with incremented version
            _sections[section.SectionName] = new SectionMetadata(section.Version + 1);
        }

        // Increment the section's version to allow multiple saves with the same instance
        section.Version++;

        // Serialize state modification and file write to prevent concurrent enumeration
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Store a deep clone to ensure immutability
            _state[section.SectionName] = section.Data.DeepClone().AsObject();
            await SaveStateToStorageAsync(_state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _stateLock.Release();
        }
    }
}
