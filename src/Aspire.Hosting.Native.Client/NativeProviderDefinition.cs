// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json;

namespace Aspire.Hosting.Native.Client;

/// <summary>Contains offline-generated portable metadata and strongly typed provider dispatch.</summary>
public sealed record NativeProviderDefinition(JsonElement Metadata,
    Func<NativeProviderContext, string, JsonElement, Task<JsonElement>> Dispatch);

/// <summary>Associates an integration-owned resource with its authoritative native declaration lifetime.</summary>
public abstract class NativeIntegrationResource(DeclaredResource declaration) : IAsyncDisposable
{
    private int _released;
    /// <summary>Gets the native declaration that anchors this integration object lifetime.</summary>
    public DeclaredResource Declaration { get; } = declaration;
    internal bool IsReleased => Volatile.Read(ref _released) != 0;
    internal async Task ReleaseAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Releases integration-owned local state when native lifetime retirement is acknowledged.</summary>
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Marks an exported integration method as an offline-projected native resource lifecycle handler.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NativeResourceLifecycleAttribute(string phase) : Attribute
{
    /// <summary>Gets initialize, allocated, ready, or retiring.</summary>
    public string Phase { get; } = phase;
}

/// <summary>Owns integration objects on one provider connection; never substitutes native model semantics.</summary>
public sealed class NativeProviderContext(NativeClient client)
{
    private readonly Dictionary<string, NativeIntegrationResource> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<NativeIntegrationResource, (string Id, string Type)> _identities = new(ReferenceEqualityComparer.Instance);
    private readonly Lock _gate = new();
    private readonly AsyncLocal<CancellationToken> _invocationCancellation = new();
    private bool _closed;
    /// <summary>Gets the transport-delivered cancellation for the current integration invocation.</summary>
    public CancellationToken CancellationToken => _invocationCancellation.Value;
    internal void SetInvocationCancellation(CancellationToken token) => _invocationCancellation.Value = token;
    /// <summary>Gets the connection used by typed provider projections.</summary>
    public NativeClient Client { get; } = client;
    /// <summary>Invokes a callback through the server's original guest authority, not by forwarding a raw guest connection.</summary>
    public Task<JsonElement> InvokeGuestCallbackAsync(string callback, Dictionary<string, JsonElement> arguments) =>
        Client.InvokeGuestCallbackAsync(callback, arguments);

    /// <summary>Publishes an integration-owned object with an explicit native declaration lifetime.</summary>
    public JsonElement PublishResource(NativeIntegrationResource resource, string typeId)
    {
        string id;
        lock (_gate)
        {
            if (_closed || resource.IsReleased)
            {
                throw new InvalidOperationException("An integration resource cannot be republished after native retirement.");
            }
            if (_identities.TryGetValue(resource, out var identity))
            {
                if (identity.Type != typeId)
                {
                    throw new InvalidOperationException("The integration resource type cannot change.");
                }
                id = identity.Id;
            }
            else
            {
                if (_resources.Count >= 4096)
                {
                    throw new InvalidOperationException("Provider resource capacity was reached.");
                }
                id = RandomNumberGenerator.GetHexString(32);
                _resources.Add(id, resource);
                _identities.Add(resource, (id, typeId));
            }
        }
        // Provider resource wire identities include a delegated native lifetime.
        // The server validates and rewraps it into caller-owned authority.
        return JsonSerializer.SerializeToElement(new NativeProviderResourceWire(id, typeId,
            Client.GetHandle(resource.Declaration)), NativeProviderJsonContext.Default.NativeProviderResourceWire);
    }

    /// <summary>Resolves a provider-local object after native connection authority validation.</summary>
    public T ReadResource<T>(JsonElement handle) where T : NativeIntegrationResource
    {
        lock (_gate)
        {
            if (handle.ValueKind != JsonValueKind.Object || !handle.TryGetProperty("$handle", out var id) ||
                id.ValueKind != JsonValueKind.String || !_resources.TryGetValue(id.GetString()!, out var value) || value is not T typed)
            {
                throw new InvalidOperationException("Unknown integration resource authority.");
            }

            return typed;
        }
    }

    internal async Task<bool> ReleaseResourceAsync(string id)
    {
        NativeIntegrationResource? resource;
        lock (_gate)
        {
            if (!_resources.Remove(id, out resource))
            {
                return true;
            }
            _identities.Remove(resource);
        }
        await resource.ReleaseAsync().ConfigureAwait(false);

        return true;
    }

    internal async Task ClearAsync()
    {
        NativeIntegrationResource[] resources;
        lock (_gate)
        {
            resources = _resources.Values.ToArray();
            _closed = true;
            _resources.Clear();
            _identities.Clear();
        }
        await Task.WhenAll(resources.Select(resource => resource.ReleaseAsync())).ConfigureAwait(false);
    }
}

internal sealed record NativeProviderResourceWire(
    [property: System.Text.Json.Serialization.JsonPropertyName("$handle")] string Handle,
    [property: System.Text.Json.Serialization.JsonPropertyName("$type")] string Type,
    [property: System.Text.Json.Serialization.JsonPropertyName("$lifetime")] JsonElement Lifetime);

[System.Text.Json.Serialization.JsonSerializable(typeof(NativeProviderResourceWire))]
internal sealed partial class NativeProviderJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
