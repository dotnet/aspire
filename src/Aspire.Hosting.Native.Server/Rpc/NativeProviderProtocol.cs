// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Native.Rpc;

/// <summary>Validates provider wire data and produces the projections consumed by invocation and lifecycle routing.</summary>
internal sealed class NativeProviderProtocol(JsonObject coreContract)
{
    private const string LifecycleScopeType = "Aspire.Hosting.Native.Server/Aspire.Hosting.Native.Api.ResourceLifecycleScope";

    public IReadOnlySet<string> CoreCapabilities { get; } = coreContract["Capabilities"]!.AsArray()
        .Select(value => value!["CapabilityId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
    public IReadOnlySet<string> CoreHandles { get; } = coreContract["HandleTypes"]!.AsArray()
        .Select(value => value!["AtsTypeId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);

    public static void ValidatePackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName) || packageName.Length > 128 ||
            packageName.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
        {
            throw RpcFault.InvalidArgument("A portable provider package identity is required.");
        }
    }

    public NativeProviderManifest ParseManifest(string packageName, JsonNode? manifest)
    {
        // Providers advertise {"protocolVersion":2,"capabilities":[{"id":"package/method",
        // "method":"method","parameters":[{"name":"resource","type":{"typeId":"package/Resource",
        // "category":"Handle"}}],"returnType":{"typeId":"void","category":"Primitive"}}]}.
        // Keep the original metadata for discovery, but route only through validated projections.
        if (manifest is not JsonObject envelope || envelope["protocolVersion"] is not JsonValue version ||
            !version.TryGetValue<int>(out var number) || number != 2 ||
            envelope["capabilities"] is not JsonArray { Count: > 0 and <= 128 } capabilities)
        {
            throw RpcFault.ProviderMetadataInvalid("Provider metadata requires protocol version 2 and bounded capabilities.");
        }
        var projected = new Dictionary<string, NativeProviderCapability>(StringComparer.Ordinal);
        foreach (var node in capabilities)
        {
            if (node is not JsonObject metadata || metadata["id"] is not JsonValue ||
                metadata["parameters"] is not JsonArray { Count: <= 32 } parameters ||
                metadata["returnType"] is not JsonObject returnType)
            {
                throw RpcFault.ProviderMetadataInvalid("Provider capability projection is incomplete.");
            }
            var id = RpcArguments.String(metadata["id"]);
            if (!id.StartsWith(packageName + "/", StringComparison.Ordinal) || id.Length > 256 ||
                string.IsNullOrWhiteSpace(RpcArguments.String(metadata["method"])) || projected.ContainsKey(id))
            {
                throw RpcFault.ProviderConflict("The provider capability identity is duplicate or outside its package.");
            }
            var resultType = ParseType(returnType, packageName);
            var arguments = new List<NativeProviderParameter>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                if (parameter is not JsonObject argument || argument["type"] is not JsonObject type ||
                    !names.Add(RpcArguments.String(argument["name"])) ||
                    string.IsNullOrWhiteSpace(RpcArguments.String(argument["name"])) ||
                    ReadFlag(argument, "isOptional") || ReadFlag(argument, "isNullable"))
                {
                    throw RpcFault.ProviderMetadataInvalid("Provider arguments require distinct required names and types.");
                }
                var name = RpcArguments.String(argument["name"]);
                if (ReadFlag(argument, "isCallback"))
                {
                    if (type["category"] is not JsonValue category || !category.TryGetValue<string>(out var categoryName) || categoryName != "Callback" ||
                        argument["callbackParameters"] is not JsonArray { Count: <= 16 } callbackParameters ||
                        argument["callbackReturnType"] is not JsonObject callbackReturn)
                    {
                        throw RpcFault.ProviderMetadataInvalid("Callback projection is incomplete.");
                    }
                    var callbackResult = ParseType(callbackReturn, packageName);
                    if (callbackResult.Kind == NativeProviderValueKind.Void)
                    {
                        throw RpcFault.ProviderMetadataUnsupported("Void callback writeback is not implemented by this provider protocol phase.");
                    }
                    var callbackTypes = new List<NativeProviderType>();
                    foreach (var callbackParameter in callbackParameters)
                    {
                        if (callbackParameter is not JsonObject projectedParameter || projectedParameter["type"] is not JsonObject projectedType)
                        {
                            throw RpcFault.ProviderMetadataInvalid("Callback projection is incomplete.");
                        }
                        callbackTypes.Add(ParseType(projectedType, packageName));
                    }
                    arguments.Add(new(name, new("callback", NativeProviderValueKind.Callback),
                        new(callbackTypes.ToArray(), callbackResult)));
                }
                else
                {
                    arguments.Add(new(name, ParseType(type, packageName), null));
                }
            }
            var targetType = metadata["targetTypeId"] is { } targetId ? RpcArguments.String(targetId) : null;
            var receiver = metadata["targetParameterName"] is { } targetName ? RpcArguments.String(targetName) : null;
            if (targetType is not null)
            {
                var target = arguments.SingleOrDefault(parameter => parameter.Name == receiver);
                if (target is null || target.Type.TypeId != targetType || target.Type.Kind != NativeProviderValueKind.Handle)
                {
                    throw RpcFault.ProviderMetadataInvalid("The provider receiver must match its projected handle parameter.");
                }
            }
            var phase = metadata["lifecyclePhase"] is { } lifecycle ? RpcArguments.String(lifecycle) : null;
            if (phase is not null && (phase is not ("initialize" or "allocated" or "ready" or "retiring") ||
                targetType is null || CoreHandles.Contains(targetType) || arguments.Count != 2 ||
                arguments.Any(parameter => parameter.Callback is not null) ||
                arguments.SingleOrDefault(parameter => parameter.Name == "scope")?.Type.TypeId != LifecycleScopeType ||
                resultType.Kind != NativeProviderValueKind.Void))
            {
                throw RpcFault.ProviderMetadataInvalid("Lifecycle metadata requires an owned receiver, one native scope and a void result.");
            }
            projected.Add(id, new(id, arguments.ToArray(), resultType, targetType, receiver, phase, metadata.DeepClone().AsObject()));
        }
        if (projected.Values.Where(capability => capability.LifecyclePhase is not null)
            .GroupBy(capability => (capability.TargetTypeId, capability.LifecyclePhase)).Any(group => group.Count() > 1))
        {
            throw RpcFault.ProviderMetadataInvalid("A provider resource phase cannot have duplicate capabilities.");
        }
        var ownTypes = projected.Values.SelectMany(capability => EnumerateTypes(capability.Metadata))
            .Where(type => type["category"]?.GetValue<string>() == "Handle")
            .Select(type => RpcArguments.String(type["typeId"])).Where(type => !CoreHandles.Contains(type)).ToHashSet(StringComparer.Ordinal);

        return new(projected, ownTypes);
    }

    private static bool ReadFlag(JsonObject metadata, string name)
    {
        if (metadata[name] is null)
        {
            return false;
        }
        if (metadata[name] is JsonValue value && value.TryGetValue<bool>(out var flag))
        {
            return flag;
        }
        throw RpcFault.ProviderMetadataInvalid("Provider parameter flags must be Boolean values.");
    }

    private NativeProviderType ParseType(JsonObject type, string packageName)
    {
        var id = RpcArguments.String(type["typeId"]);
        var category = RpcArguments.String(type["category"]);
        if (category == "Handle" && (CoreHandles.Contains(id) ||
            id.StartsWith(packageName + "/", StringComparison.Ordinal) && id.Length <= 256))
        {
            return new(id, NativeProviderValueKind.Handle);
        }
        var kind = category == "Primitive" ? id switch
        {
            "void" => NativeProviderValueKind.Void,
            "string" => NativeProviderValueKind.String,
            "bool" => NativeProviderValueKind.Boolean,
            "int" => NativeProviderValueKind.Int32,
            "long" => NativeProviderValueKind.Int64,
            _ => (NativeProviderValueKind?)null
        } : null;
        if (kind is null)
        {
            throw RpcFault.ProviderMetadataUnsupported("This provider protocol phase supports primitive values, typed handles and projected callbacks only.");
        }

        return new(id, kind.Value);
    }

    public static void ValidatePrimitive(JsonNode? value, NativeProviderType type)
    {
        switch (type.Kind)
        {
            case NativeProviderValueKind.Void when value is null: return;
            case NativeProviderValueKind.String: _ = RpcArguments.String(value); return;
            case NativeProviderValueKind.Boolean: _ = RpcArguments.Boolean(value); return;
            case NativeProviderValueKind.Int32: _ = RpcArguments.Int32(value); return;
            case NativeProviderValueKind.Int64: _ = RpcArguments.Int64(value); return;
            default: throw RpcFault.TypeMismatch("The provider value does not match its projection.");
        }
    }

    public NativeProviderHandle ParseReturnedHandle(JsonNode? value, NativeProviderType type)
    {
        // Core handles are {"$handle":"id","$type":"core/Type"}; provider-owned
        // handles also require "$lifetime" containing delegated native declaration authority.
        var handle = value as JsonObject ?? throw RpcFault.InvalidHandle("The provider did not return a typed handle.");
        var core = CoreHandles.Contains(type.TypeId);
        RpcArguments.Validate(handle, core ? ["$handle", "$type"] : ["$handle", "$type", "$lifetime"]);
        var id = RpcArguments.String(handle["$handle"]);
        if (id.Length is 0 or > 512 || RpcArguments.String(handle["$type"]) != type.TypeId)
        {
            throw RpcFault.TypeMismatch("The provider returned an invalid resource handle.");
        }
        if (!core && handle["$lifetime"] is not JsonObject)
        {
            throw RpcFault.InvalidHandle("Integration resource lifetime must be delegated by its caller.");
        }

        return new(id, handle, handle["$lifetime"] as JsonObject);
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

internal enum NativeProviderValueKind
{
    Void,
    String,
    Boolean,
    Int32,
    Int64,
    Handle,
    Callback
}
internal sealed record NativeProviderType(string TypeId, NativeProviderValueKind Kind);
internal sealed record NativeProviderCallback(NativeProviderType[] Parameters, NativeProviderType ReturnType);
internal sealed record NativeProviderParameter(string Name, NativeProviderType Type, NativeProviderCallback? Callback);
internal sealed record NativeProviderCapability(string Id, NativeProviderParameter[] Parameters, NativeProviderType ReturnType,
    string? TargetTypeId, string? TargetParameterName, string? LifecyclePhase, JsonObject Metadata);
internal sealed record NativeProviderManifest(Dictionary<string, NativeProviderCapability> Capabilities, HashSet<string> OwnedTypes);
internal sealed record NativeProviderHandle(string Id, JsonObject Wire, JsonObject? Lifetime);
