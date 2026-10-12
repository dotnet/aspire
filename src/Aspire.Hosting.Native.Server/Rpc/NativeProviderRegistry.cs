// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Aspire.Hosting.Native.Api;
using Aspire.Hosting.Native.Server;
using Microsoft.Extensions.Logging;
using Aspire.Hosting.Native.Runtime;

namespace Aspire.Hosting.Native.Rpc;

/// <summary>Owns explicit provider activation separately from message framing and static AOT dispatch.</summary>
internal interface INativeProviderRegistry : IProviderLifecycleDispatcher
{
    string Expect(NativeRpcConnection owner, string packageName);
    Task RegisterAsync(NativeRpcConnection connection, string authority, INativeRpcPeer peer);
    Task WaitRegistrationAsync(NativeRpcConnection owner, string authority);
    bool IsProvider(NativeRpcConnection connection);
    JsonObject InspectCapabilities();
    Task<JsonNode?> InvokeAsync(NativeRpcConnection caller, string capability, JsonObject arguments);
    Task<JsonNode?> InvokeCoreAsync(NativeRpcConnection provider, string capability, JsonObject arguments);
    Task<JsonNode?> InvokeCallbackAsync(NativeRpcConnection provider, string callback, JsonObject arguments);
    void Disconnect(NativeRpcConnection connection);
}

internal sealed partial class NativeProviderRegistry(JsonObject coreContract, NativeServerOptions options, ILogger logger) : INativeProviderRegistry
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Expectation> _expected = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Expectation> _activationWaits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Registration> _capabilities = new(StringComparer.Ordinal);
    private readonly Dictionary<NativeRpcConnection, Registration> _providers = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> _providerTypes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _coreCapabilities = coreContract["Capabilities"]!.AsArray()
        .Select(value => value!["CapabilityId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
    private readonly HashSet<string> _coreHandles = coreContract["HandleTypes"]!.AsArray()
        .Select(value => value!["AtsTypeId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);

    public string Expect(NativeRpcConnection owner, string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName) || packageName.Length > 128 ||
            packageName.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
        {
            throw new RpcFault("INVALID_ARGUMENT", "A portable provider package identity is required.");
        }
        lock (_gate)
        {
            foreach (var (authority, expectation) in _activationWaits.ToArray())
            {
                if (options.TimeProvider.GetElapsedTime(expectation.Created) >= options.ProviderRegistrationTimeout)
                {
                    _expected.Remove(authority);
                    _activationWaits.Remove(authority);
                    expectation.Completion.TrySetCanceled();
                }
            }
            if (_activationWaits.Count >= 64 || _providers.Count >= 32)
            {
                throw new RpcFault("PROVIDER_LIMIT", "Provider registration capacity was reached.");
            }
            var token = RandomNumberGenerator.GetHexString(64);
            var pending = new Expectation(owner, packageName, options.TimeProvider.GetTimestamp());
            _expected.Add(token, pending);
            _activationWaits.Add(token, pending);

            return token;
        }
    }

    public async Task RegisterAsync(NativeRpcConnection connection, string authority, INativeRpcPeer peer)
    {
        Expectation expected;
        lock (_gate)
        {
            // An expected authority is one-use even when discovery subsequently
            // fails. The registration process never guesses a provider by package.
            if (!_expected.Remove(authority, out expected!) || expected.Owner.IsClosed ||
                ReferenceEquals(expected.Owner, connection) || _providers.ContainsKey(connection) ||
                options.TimeProvider.GetElapsedTime(expected.Created) >= options.ProviderRegistrationTimeout)
            {
                throw new RpcFault("PROVIDER_REGISTRATION_REJECTED", "The expected provider registration is unavailable.");
            }
        }
        try
        {
            using var deadline = new CancellationTokenSource(options.ProviderRegistrationTimeout, options.TimeProvider);
            using var discovery = CancellationTokenSource.CreateLinkedTokenSource(connection.Closed, deadline.Token);
            var manifest = await peer.RequestAsync("getCapabilities", [], discovery.Token).ConfigureAwait(false);
            var projected = ValidateManifest(expected.PackageName, manifest);
            var ownTypes = projected.Values.SelectMany(EnumerateTypes)
                .Where(type => type["category"]?.GetValue<string>() == "Handle")
                .Select(type => RpcArguments.String(type["typeId"])).Where(type => !_coreHandles.Contains(type)).ToHashSet(StringComparer.Ordinal);
            var registration = new Registration(expected.Owner, connection, peer, projected);
            lock (_gate)
            {
                if (expected.Owner.IsClosed || connection.IsClosed || peer.Closed.IsCancellationRequested ||
                    _capabilities.Count + projected.Count > 4096 ||
                    projected.Keys.Any(id => _coreCapabilities.Contains(id) || _capabilities.ContainsKey(id)) ||
                    ownTypes.Overlaps(_providerTypes) ||
                    _providers.ContainsKey(connection))
                {
                    throw new RpcFault("PROVIDER_CONFLICT", "The provider registration conflicts with an existing capability.");
                }
                foreach (var id in projected.Keys)
                {
                    _capabilities.Add(id, registration);
                }
                _providers.Add(connection, registration);
                _providerTypes.UnionWith(ownTypes);
            }
            logger.LogInformation("Native provider activated with {CapabilityCount} capabilities.", projected.Count);
            expected.Completion.TrySetResult();
        }
        catch
        {
            expected.Completion.TrySetException(new RpcFault("PROVIDER_REGISTRATION_REJECTED", "The provider activation failed."));
            _ = expected.Completion.Task.Exception;
            throw;
        }
    }

    public async Task WaitRegistrationAsync(NativeRpcConnection owner, string authority)
    {
        Expectation expected;
        lock (_gate)
        {
            if (!_activationWaits.TryGetValue(authority, out expected!) || !ReferenceEquals(owner, expected.Owner))
            {
                throw new RpcFault("PROVIDER_REGISTRATION_REJECTED", "The expected activation authority is unavailable.");
            }
        }
        try
        {
            await expected.Completion.Task.WaitAsync(options.ProviderRegistrationTimeout, options.TimeProvider, owner.Closed).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new RpcFault("PROVIDER_TIMEOUT", "The provider activation deadline was reached.");
        }
        finally
        {
            lock (_gate)
            {
                _activationWaits.Remove(authority);
                _expected.Remove(authority);
            }
        }
    }

    public bool IsProvider(NativeRpcConnection connection)
    {
        lock (_gate)
        {
            return _providers.ContainsKey(connection);
        }
    }

    public JsonObject InspectCapabilities()
    {
        lock (_gate)
        {
            return new JsonObject
            {
                ["protocolVersion"] = 2,
                ["capabilities"] = new JsonArray(_capabilities.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => (JsonNode?)pair.Value.Capabilities[pair.Key].DeepClone()).ToArray())
            };
        }
    }

    public async Task<JsonNode?> InvokeAsync(NativeRpcConnection caller, string capability, JsonObject arguments)
    {
        // Connection dispatch starts under its handle gate. Yield before taking
        // another connection's gate to avoid cross-connection lock inversion.
        await Task.Yield();
        Registration registration;
        lock (_gate)
        {
            if (!_capabilities.TryGetValue(capability, out registration!))
            {
                throw new RpcFault("UNKNOWN_CAPABILITY", "The capability is not implemented.");
            }
        }
        EnsureAvailable(registration);
        var metadata = registration.Capabilities[capability];
        if (metadata["lifecyclePhase"] is not null)
        {
            throw new RpcFault("LIFECYCLE_AUTHORITY_REQUIRED", "Lifecycle capabilities require native phase authority.");
        }
        var parameters = metadata["parameters"]!.AsArray();
        RpcArguments.Validate(arguments, parameters.Select(parameter => parameter!["name"]!.GetValue<string>()).ToArray());
        var mapped = new JsonObject();
        foreach (var parameter in parameters.Cast<JsonObject>())
        {
            var name = parameter["name"]!.GetValue<string>();
            if (parameter["isCallback"]?.GetValue<bool>() == true)
            {
                var callbackId = RpcArguments.String(arguments[name]);
                var callback = new ProviderCallback(caller, callbackId, parameter);
                mapped[name] = registration.Connection.AddCallback(callback);
            }
            else
            {
                mapped[name] = ToProvider(registration, caller, arguments[name], parameter["type"]!.AsObject());
            }
        }
        try
        {
            var result = await registration.Peer.RequestAsync("handleExternalCapability",
                new JsonArray(JsonValue.Create(capability), mapped, JsonValue.Create(Guid.NewGuid().ToString())), caller.Closed).ConfigureAwait(false);
            EnsureAvailable(registration);

            return ToCaller(registration, caller, result, metadata["returnType"]!.AsObject());
        }
        catch (RpcFault fault) when (fault.FaultCode == "PROVIDER_TIMEOUT")
        {
            Disconnect(registration.Connection);
            throw;
        }
        catch (OperationCanceledException)
        {
            Disconnect(registration.Connection);
            throw new RpcFault("PROVIDER_DISCONNECTED", "The provider invocation did not complete; it will not be replayed.");
        }
    }

    public async Task<JsonNode?> InvokeCoreAsync(NativeRpcConnection provider, string capability, JsonObject arguments)
    {
        await Task.Yield();
        Registration registration;
        lock (_gate)
        {
            if (!_providers.TryGetValue(provider, out registration!))
            {
                throw new RpcFault("PROVIDER_DISCONNECTED", "The provider authority is no longer available.");
            }
        }
        EnsureAvailable(registration);
        NativeRpcConnection? origin = null;
        JsonNode? Translate(JsonNode? value)
        {
            if (value is JsonObject handle && handle.ContainsKey("$handle"))
            {
                var reference = provider.ReadReference(handle);
                if (reference is ResourceLifecycleScope)
                {
                    if (origin is not null && !ReferenceEquals(origin, provider))
                    {
                        throw new RpcFault("INVALID_HANDLE", "A lifecycle scope cannot combine general declaration authority.");
                    }
                    origin = provider;

                    return handle.DeepClone();
                }
                if (reference is not DelegatedCoreReference delegated)
                {
                    throw new RpcFault("TYPE_MISMATCH", "Core calls require delegated core authority.");
                }
                if (origin is not null && !ReferenceEquals(origin, delegated.Owner))
                {
                    throw new RpcFault("INVALID_HANDLE", "A core call cannot combine different connection authorities.");
                }
                origin = delegated.Owner;
                delegated.Owner.ReadReference(delegated.Handle);

                return delegated.Handle.DeepClone();
            }
            return value switch
            {
                JsonObject obj => new JsonObject(obj.Select(pair => KeyValuePair.Create(pair.Key, Translate(pair.Value)))),
                JsonArray array => new JsonArray(array.Select(Translate).ToArray()),
                _ => value?.DeepClone()
            };
        }
        var translated = (JsonObject)Translate(arguments)!;
        if (origin is null)
        {
            throw new RpcFault("INVALID_HANDLE", "Provider core calls require delegated connection authority.");
        }
        var result = await origin.InvokeCoreAsync(capability, translated).ConfigureAwait(false);
        return DelegateResult(result);

        JsonNode? DelegateResult(JsonNode? value)
        {
            if (value is JsonObject handle && handle.ContainsKey("$handle"))
            {
                origin.ReadReference(handle);

                return provider.AddReference(new DelegatedCoreReference(origin, handle.DeepClone().AsObject()),
                    RpcArguments.String(handle["$type"]));
            }
            return value switch
            {
                JsonObject obj => new JsonObject(obj.Select(pair => KeyValuePair.Create(pair.Key, DelegateResult(pair.Value)))),
                JsonArray array => new JsonArray(array.Select(DelegateResult).ToArray()),
                _ => value?.DeepClone()
            };
        }
    }

    public async Task<JsonNode?> InvokeCallbackAsync(NativeRpcConnection provider, string callbackId, JsonObject arguments)
    {
        await Task.Yield();
        Registration registration;
        lock (_gate)
        {
            if (!_providers.TryGetValue(provider, out registration!))
            {
                throw new RpcFault("INVALID_HANDLE", "Callbacks require a registered provider.");
            }
        }
        EnsureAvailable(registration);
        var callback = provider.ReadCallback(callbackId);
        var parameters = callback.Metadata["callbackParameters"]!.AsArray();
        RpcArguments.Validate(arguments, Enumerable.Range(0, parameters.Count).Select(index => $"p{index}").ToArray());
        var mapped = new JsonObject();
        for (var index = 0; index < parameters.Count; index++)
        {
            mapped[$"p{index}"] = ToCaller(registration, callback.Owner, arguments[$"p{index}"],
                parameters[index]!["type"]!.AsObject());
        }
        var result = await callback.Owner.RequestAsync("invokeCallback",
            new JsonArray(JsonValue.Create(callback.Id), mapped)).ConfigureAwait(false);
        return ToProvider(registration, callback.Owner, result, callback.Metadata["callbackReturnType"]!.AsObject());
    }

    private JsonNode? ToProvider(Registration registration, NativeRpcConnection caller, JsonNode? value, JsonObject type)
    {
        if (type["category"]!.GetValue<string>() != "Handle")
        {
            ValidatePrimitive(value, type);
            return value?.DeepClone();
        }
        var handle = value as JsonObject ?? throw new RpcFault("INVALID_HANDLE", "A typed handle is required.");
        var expectedType = RpcArguments.String(type["typeId"]);
        var reference = caller.ReadReference(handle, expectedType);
        if (reference is ProviderResourceReference external)
        {
            if (!ReferenceEquals(external.Provider, registration) || !ReferenceEquals(external.Owner, caller))
            {
                throw new RpcFault("INVALID_HANDLE", "The provider resource belongs to another authority.");
            }
            return external.Handle.DeepClone();
        }
        if (!_coreHandles.Contains(expectedType))
        {
            throw new RpcFault("TYPE_MISMATCH", "The value is not an integration-owned resource.");
        }
        return registration.Connection.AddReference(new DelegatedCoreReference(caller, handle.DeepClone().AsObject()), expectedType);
    }

    private JsonNode? ToCaller(Registration registration, NativeRpcConnection caller, JsonNode? value, JsonObject type)
    {
        if (type["category"]!.GetValue<string>() != "Handle")
        {
            ValidatePrimitive(value, type);
            return value?.DeepClone();
        }
        var expectedType = RpcArguments.String(type["typeId"]);
        var handle = value as JsonObject ?? throw new RpcFault("INVALID_HANDLE", "The provider did not return a typed handle.");
        var coreHandle = _coreHandles.Contains(expectedType);
        RpcArguments.Validate(handle, coreHandle ? ["$handle", "$type"] : ["$handle", "$type", "$lifetime"]);
        var id = RpcArguments.String(handle["$handle"]);
        if (id.Length is 0 or > 512 || RpcArguments.String(handle["$type"]) != expectedType)
        {
            throw new RpcFault("TYPE_MISMATCH", "The provider returned an invalid resource handle.");
        }
        if (coreHandle)
        {
            if (registration.Connection.ReadReference(handle, expectedType) is not DelegatedCoreReference delegated ||
                !ReferenceEquals(delegated.Owner, caller))
            {
                throw new RpcFault("INVALID_HANDLE", "The returned core handle is not delegated by this caller.");
            }
            caller.ReadReference(delegated.Handle, expectedType);
            return delegated.Handle.DeepClone();
        }
        if (handle["$lifetime"] is not JsonObject lifetime ||
            registration.Connection.ReadReference(lifetime) is not DelegatedCoreReference backing ||
            !ReferenceEquals(backing.Owner, caller))
        {
            throw new RpcFault("INVALID_HANDLE", "Integration resource lifetime must be delegated by its caller.");
        }
        if (caller.ReadReference(backing.Handle) is not DeclaredResource declaration)
        {
            throw new RpcFault("INVALID_HANDLE", "Integration resource lifetime must be a native resource declaration.");
        }
        TrackProviderObject(registration, caller, handle, declaration, expectedType);
        return caller.AddReference(new ProviderResourceReference(registration, caller, handle.DeepClone().AsObject(),
            backing.Handle.DeepClone().AsObject()), expectedType);
    }

    private Dictionary<string, JsonObject> ValidateManifest(string packageName, JsonNode? manifest)
    {
        if (manifest is not JsonObject envelope || envelope["protocolVersion"]?.GetValue<int>() != 2 ||
            envelope["capabilities"] is not JsonArray { Count: > 0 and <= 128 } capabilities)
        {
            throw new RpcFault("PROVIDER_METADATA_INVALID", "Provider metadata requires protocol version 2 and bounded capabilities.");
        }
        var result = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var node in capabilities)
        {
            if (node is not JsonObject metadata || metadata["id"] is not JsonValue ||
                metadata["parameters"] is not JsonArray { Count: <= 32 } parameters ||
                metadata["returnType"] is not JsonObject returnType)
            {
                throw new RpcFault("PROVIDER_METADATA_INVALID", "Provider capability projection is incomplete.");
            }
            var id = RpcArguments.String(metadata["id"]);
            if (!id.StartsWith(packageName + "/", StringComparison.Ordinal) || id.Length > 256 ||
                string.IsNullOrWhiteSpace(RpcArguments.String(metadata["method"])) || !result.TryAdd(id, metadata.DeepClone().AsObject()))
            {
                throw new RpcFault("PROVIDER_CONFLICT", "The provider capability identity is duplicate or outside its package.");
            }
            ValidateType(returnType, packageName);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                if (parameter is not JsonObject argument || argument["type"] is not JsonObject type ||
                    !names.Add(RpcArguments.String(argument["name"])) ||
                    string.IsNullOrWhiteSpace(RpcArguments.String(argument["name"])) ||
                    argument["isOptional"]?.GetValue<bool>() == true || argument["isNullable"]?.GetValue<bool>() == true)
                {
                    throw new RpcFault("PROVIDER_METADATA_INVALID", "Provider arguments require distinct required names and types.");
                }
                if (argument["isCallback"]?.GetValue<bool>() == true)
                {
                    if (type["category"]?.GetValue<string>() != "Callback" ||
                        argument["callbackParameters"] is not JsonArray { Count: <= 16 } callbackParameters ||
                        argument["callbackReturnType"] is not JsonObject callbackReturn)
                    {
                        throw new RpcFault("PROVIDER_METADATA_INVALID", "Callback projection is incomplete.");
                    }
                    ValidateType(callbackReturn, packageName);
                    if (RpcArguments.String(callbackReturn["typeId"]) == "void")
                    {
                        throw new RpcFault("PROVIDER_METADATA_UNSUPPORTED", "Void callback writeback is not implemented by this provider protocol phase.");
                    }
                    foreach (var callbackParameter in callbackParameters)
                    {
                        ValidateType(callbackParameter!["type"]!.AsObject(), packageName);
                    }
                }
                else
                {
                    ValidateType(type, packageName);
                }
            }
            if (metadata["targetTypeId"] is { } targetId)
            {
                var receiver = RpcArguments.String(metadata["targetParameterName"]);
                var target = parameters.SingleOrDefault(parameter => parameter!["name"]!.GetValue<string>() == receiver);
                if (target?["type"]?["typeId"]?.GetValue<string>() != RpcArguments.String(targetId) ||
                    target?["type"]?["category"]?.GetValue<string>() != "Handle")
                {
                    throw new RpcFault("PROVIDER_METADATA_INVALID", "The provider receiver must match its projected handle parameter.");
                }
            }
            if (metadata["lifecyclePhase"] is not null)
            {
                ValidateLifecycleMetadata(metadata);
            }
        }
        var phases = result.Values.Where(metadata => metadata["lifecyclePhase"] is not null);
        if (phases.GroupBy(metadata => (Type: RpcArguments.String(metadata["targetTypeId"]),
                Phase: RpcArguments.String(metadata["lifecyclePhase"]))).Any(group => group.Count() > 1))
        {
            throw new RpcFault("PROVIDER_METADATA_INVALID", "A provider resource phase cannot have duplicate capabilities.");
        }

        return result;
    }

    private void ValidateType(JsonObject type, string packageName)
    {
        var id = RpcArguments.String(type["typeId"]);
        var category = RpcArguments.String(type["category"]);
        if (category == "Primitive" && id is "string" or "bool" or "int" or "long" or "void" ||
            category == "Handle" && (_coreHandles.Contains(id) || id.StartsWith(packageName + "/", StringComparison.Ordinal) && id.Length <= 256))
        {
            return;
        }
        throw new RpcFault("PROVIDER_METADATA_UNSUPPORTED", "This provider protocol phase supports primitive values, typed handles and projected callbacks only.");
    }

    private static void ValidatePrimitive(JsonNode? value, JsonObject type)
    {
        var id = RpcArguments.String(type["typeId"]);
        switch (id)
        {
            case "void" when value is null: return;
            case "string": _ = RpcArguments.String(value); return;
            case "bool": _ = RpcArguments.Boolean(value); return;
            case "int": _ = RpcArguments.Int32(value); return;
            case "long": _ = RpcArguments.Int64(value); return;
            default: throw new RpcFault("TYPE_MISMATCH", "The provider value does not match its projection.");
        }
    }

    private static void EnsureAvailable(Registration registration)
    {
        if (registration.Disconnected || registration.Owner.IsClosed || registration.Connection.IsClosed || registration.Peer.Closed.IsCancellationRequested)
        {
            throw new RpcFault("PROVIDER_DISCONNECTED", "The provider is no longer available.");
        }
    }

    public void Disconnect(NativeRpcConnection connection)
    {
        var peers = new List<INativeRpcPeer>();
        lock (_gate)
        {
            foreach (var (id, expected) in _activationWaits.ToArray())
            {
                if (ReferenceEquals(expected.Owner, connection))
                {
                    _expected.Remove(id);
                    _activationWaits.Remove(id);
                    expected.Completion.TrySetCanceled();
                }
            }
            foreach (var (provider, registration) in _providers.ToArray())
            {
                if (registration.Matches(connection))
                {
                    registration.Disconnected = true;
                    peers.Add(registration.Peer);
                    _providers.Remove(provider);
                    registration.Release();
                }
            }
        }
        // Closing a peer invokes its connection-disposal callback. Do not hold
        // the registry gate across callbacks that acquire another connection gate.
        foreach (var peer in peers)
        {
            peer.Dispose();
        }
    }

    internal sealed class Registration(NativeRpcConnection owner, NativeRpcConnection connection, INativeRpcPeer peer,
        Dictionary<string, JsonObject> capabilities)
    {
        private NativeRpcConnection? _owner = owner;
        private NativeRpcConnection? _connection = connection;
        private INativeRpcPeer? _peer = peer;
        public NativeRpcConnection Owner => _owner ?? throw Unavailable();
        public NativeRpcConnection Connection => _connection ?? throw Unavailable();
        public INativeRpcPeer Peer => _peer ?? throw Unavailable();
        public Dictionary<string, JsonObject> Capabilities { get; } = capabilities;
        public Guid Id { get; } = Guid.NewGuid();
        public Dictionary<string, PublishedResource> Resources { get; } = new(StringComparer.Ordinal);
        public bool Disconnected { get; set; }
        public bool Matches(NativeRpcConnection candidate) =>
            ReferenceEquals(_owner, candidate) || ReferenceEquals(_connection, candidate);
        public void Release()
        {
            // Keep metadata tombstones for deterministic stale-route rejection,
            // without retaining the disconnected provider's live authority tables.
            _owner = null;
            _connection = null;
            _peer = null;
            foreach (var resource in Resources.Values)
            {
                resource.IsRetired = true;
            }
            Resources.Clear();
        }
        private static RpcFault Unavailable() => new("PROVIDER_DISCONNECTED", "The integration provider is unavailable.");
    }

    private sealed record Expectation(NativeRpcConnection Owner, string PackageName, long Created)
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static IEnumerable<JsonObject> EnumerateTypes(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("typeId") && obj.ContainsKey("category"))
            {
                yield return obj;
            }
            foreach (var nested in obj.SelectMany(pair => EnumerateTypes(pair.Value)))
            {
                yield return nested;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var nested in array.SelectMany(EnumerateTypes))
            {
                yield return nested;
            }
        }
    }
}

/// <summary>Delegates invocation access, never ownership, to the caller's original core handle table.</summary>
internal sealed record DelegatedCoreReference(NativeRpcConnection Owner, JsonObject Handle) : ICapabilityLifetime
{
    public bool IsRevoked => Owner.IsClosed || !Owner.IsReferenceAlive(Handle);
}

internal sealed record ProviderResourceReference(NativeProviderRegistry.Registration Provider, NativeRpcConnection Owner,
    JsonObject Handle, JsonObject Lifetime) : ICapabilityLifetime
{
    public bool IsRevoked => Owner.IsClosed || Provider.Disconnected || Provider.Connection.IsClosed || !Owner.IsReferenceAlive(Lifetime);
}

internal sealed record ProviderCallback(NativeRpcConnection Owner, string Id, JsonObject Metadata);
