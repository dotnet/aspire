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
/// Initializes a new instance of the <see cref="DeploymentStateManagerBase{T}"/> class.
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
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly object _sectionsLock = new();
    private readonly Dictionary<string, SectionMetadata> _sections = new();
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
    /// Loads the deployment state from storage, using caching to avoid repeated loads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded state as a JsonObject.</returns>
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
                _state = JsonFlattener.UnflattenJsonObject(flattenedState);
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

    /// <inheritdoc/>
    public async Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveStateToStorageAsync(state, cancellationToken).ConfigureAwait(false);
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
