// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Native.Runtime;

namespace Aspire.Hosting.Native.Rpc;

/// <summary>Validates provider wire data and produces the projections consumed by invocation and lifecycle routing.</summary>
internal sealed class NativeProviderProtocol(JsonObject coreContract)
{
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
        var capabilities = NativeProviderMetadata.Read(manifest);
        var projected = new Dictionary<string, NativeProviderCapability>(StringComparer.Ordinal);
        var ownTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var metadata in capabilities)
        {
            var id = metadata.Id;
            if (!id.StartsWith(packageName + "/", StringComparison.Ordinal) || id.Length > 256 ||
                string.IsNullOrWhiteSpace(metadata.Method) || projected.ContainsKey(id))
            {
                throw RpcFault.ProviderConflict("The provider capability identity is duplicate or outside its package.");
            }
            var resultType = ProjectType(metadata.ReturnType);
            var arguments = new List<NativeProviderParameter>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in metadata.Parameters)
            {
                if (!names.Add(parameter.Name) || string.IsNullOrWhiteSpace(parameter.Name))
                {
                    throw RpcFault.ProviderMetadataInvalid("Provider arguments require distinct required names and types.");
                }
                if (parameter.Callback is { } callback)
                {
                    var callbackResult = ProjectType(callback.ReturnType);
                    if (callbackResult.Kind == NativeProviderValueKind.Void)
                    {
                        throw RpcFault.ProviderMetadataUnsupported("Void callback writeback is not implemented by this provider protocol phase.");
                    }
                    arguments.Add(new(parameter.Name, new("callback", NativeProviderValueKind.Callback),
                        new(callback.Parameters.Select(ProjectType).ToArray(), callbackResult)));
                }
                else
                {
                    arguments.Add(new(parameter.Name, ProjectType(parameter.Type), null));
                }
            }
            var targetType = metadata.TargetTypeId;
            var receiver = metadata.TargetParameterName;
            if (targetType is not null)
            {
                var target = arguments.SingleOrDefault(parameter => parameter.Name == receiver);
                if (target is null || target.Type.TypeId != targetType || target.Type.Kind != NativeProviderValueKind.Handle)
                {
                    throw RpcFault.ProviderMetadataInvalid("The provider receiver must match its projected handle parameter.");
                }
            }
            var phase = metadata.LifecyclePhase;
            if (phase is not null && (phase is not (ProviderLifecyclePhases.Initialize or ProviderLifecyclePhases.Allocated or ProviderLifecyclePhases.Ready or ProviderLifecyclePhases.Retiring) ||
                targetType is null || CoreHandles.Contains(targetType) || arguments.Count != 2 ||
                arguments.Any(parameter => parameter.Callback is not null) ||
                arguments.SingleOrDefault(parameter => parameter.Name == "scope")?.Type.TypeId != NativeRpcContract.LifecycleScopeTypeId ||
                resultType.Kind != NativeProviderValueKind.Void))
            {
                throw RpcFault.ProviderMetadataInvalid("Lifecycle metadata requires an owned receiver, one native scope and a void result.");
            }
            projected.Add(id, new(id, arguments.ToArray(), resultType, targetType, receiver, phase, metadata.Wire.DeepClone().AsObject()));
        }
        if (projected.Values.Where(capability => capability.LifecyclePhase is not null)
            .GroupBy(capability => (capability.TargetTypeId, capability.LifecyclePhase)).Any(group => group.Count() > 1))
        {
            throw RpcFault.ProviderMetadataInvalid("A provider resource phase cannot have duplicate capabilities.");
        }
        return new(projected, ownTypes);

        NativeProviderType ProjectType(NativeProviderMetadataType metadataType)
        {
            var type = ParseType(metadataType, packageName);
            if (type.Kind == NativeProviderValueKind.Handle && !CoreHandles.Contains(type.TypeId))
            {
                ownTypes.Add(type.TypeId);
            }

            return type;
        }
    }

    private NativeProviderType ParseType(NativeProviderMetadataType type, string packageName)
    {
        var id = type.TypeId;
        if (type.Category == NativeProviderTypeCategory.Handle && (CoreHandles.Contains(id) ||
            id.StartsWith(packageName + "/", StringComparison.Ordinal) && id.Length <= 256))
        {
            return new(id, NativeProviderValueKind.Handle);
        }
        var kind = type.Category == NativeProviderTypeCategory.Primitive ? id switch
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
