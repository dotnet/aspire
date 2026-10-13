// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Hosting.CodeGeneration.TypeScript;
using Aspire.TypeSystem;

namespace Aspire.Managed;

/// <summary>Projects portable provider contracts with the normal ATS generator, outside the execution server.</summary>
internal static class AppHostServerV2CodeGeneration
{
    internal static async Task<int> GenerateAsync(string requestPath)
    {
        var request = JsonNode.Parse(await File.ReadAllTextAsync(requestPath).ConfigureAwait(false))!.AsObject();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var core = JsonSerializer.Deserialize<AtsContext>(
            await File.ReadAllTextAsync(request["coreContract"]!.GetValue<string>()).ConfigureAwait(false), options)
            ?? throw new InvalidDataException("AppHost Server v2 is missing its core ATS contract.");
        var capabilities = core.Capabilities.ToList();
        var handles = core.HandleTypes.ToDictionary(type => type.AtsTypeId, StringComparer.Ordinal);
        var ids = capabilities.Select(capability => capability.CapabilityId).ToHashSet(StringComparer.Ordinal);
        var sdkFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var provider in request["providers"]!.AsArray())
        {
            var metadata = JsonNode.Parse(await File.ReadAllTextAsync(provider!["metadata"]!.GetValue<string>()).ConfigureAwait(false))!.AsObject();
            if (metadata["protocolVersion"]?.GetValue<int>() != 2 ||
                metadata["capabilities"] is not JsonArray { Count: > 0 and <= 128 } exports)
            {
                throw new InvalidDataException("AppHost Server v2 providers require a bounded protocol-v2 ATS contract.");
            }
            var packageName = provider["packageName"]!.GetValue<string>();
            foreach (var export in exports)
            {
                if (export!["lifecyclePhase"] is not null)
                {
                    continue;
                }
                // Portable protocol-v2 capabilities use id/method; ATS uses
                // CapabilityId/MethodName. The type and callback projections are shared.
                var projection = export.DeepClone().AsObject();
                projection["capabilityId"] = projection["id"]!.DeepClone();
                projection["methodName"] = projection["method"]!.DeepClone();
                var capability = projection.Deserialize<AtsCapabilityInfo>(options)
                    ?? throw new InvalidDataException("Invalid portable ATS capability.");
                if (!capability.CapabilityId.StartsWith(packageName + "/", StringComparison.Ordinal) ||
                    !ids.Add(capability.CapabilityId))
                {
                    throw new InvalidDataException($"Provider capability '{capability.CapabilityId}' is duplicate or outside its package.");
                }
                capabilities.Add(capability);
                foreach (var type in capability.Parameters.Select(parameter => parameter.Type)
                    .Append(capability.ReturnType).Where(type => type is not null))
                {
                    AddHandle(type!);
                }
                foreach (var parameter in capability.Parameters.Where(parameter => parameter.IsCallback))
                {
                    foreach (var callbackParameter in parameter.CallbackParameters ?? [])
                    {
                        AddHandle(callbackParameter.Type);
                    }
                    if (parameter.CallbackReturnType is { } callbackReturn)
                    {
                        AddHandle(callbackReturn);
                    }
                }
            }
            foreach (var file in provider["sdkFiles"]!.AsObject())
            {
                if (Path.GetFileName(file.Key) != file.Key || !file.Key.EndsWith(".mts", StringComparison.Ordinal) ||
                    file.Key is "native-builder.mts" or "native-client.mts" or "native-types.mts" ||
                    !sdkFiles.TryAdd(file.Key, await File.ReadAllTextAsync(file.Value!.GetValue<string>()).ConfigureAwait(false)))
                {
                    throw new InvalidDataException($"Provider SDK file '{file.Key}' is invalid or duplicated.");
                }
            }
        }
        var context = new AtsContext
        {
            Capabilities = capabilities,
            HandleTypes = handles.Values.ToArray(),
            DtoTypes = core.DtoTypes,
            EnumTypes = core.EnumTypes,
            ExportedValues = core.ExportedValues
        };
        var files = new AtsTypeScriptCodeGenerator().GenerateDistributedApplication(context, includeManagedBootstrap: false);
        var output = request["output"]!.GetValue<string>();
        Directory.CreateDirectory(output);
        foreach (var (name, content) in files)
        {
            if (sdkFiles.ContainsKey(name))
            {
                throw new InvalidDataException($"Provider SDK file '{name}' replaces a core SDK file.");
            }
            var projected = content;
            if (name == "aspire.mts")
            {
                projected += "\nexport { createBuilder, DistributedApplicationBuilder, ResourceBuilder, Resource, ContainerResource, ExecutableResource, ParameterResource, ReferenceExpression, EndpointReference, ExecutableCommand, NativeBuildContext, NativeApplication } from './native-builder.mjs';\n";
                foreach (var provider in request["providers"]!.AsArray())
                {
                    foreach (var entryPoint in provider!["exports"]!.AsArray())
                    {
                        var module = entryPoint!.GetValue<string>();
                        if (!sdkFiles.ContainsKey(module))
                        {
                            throw new InvalidDataException($"Provider entry point '{module}' is not an SDK file.");
                        }
                        projected += "export * from " + JsonSerializer.Serialize("./" + module[..^4] + ".mjs") + ";\n";
                    }
                }
            }
            await File.WriteAllTextAsync(Path.Combine(output, name), projected).ConfigureAwait(false);
        }
        foreach (var (name, content) in sdkFiles)
        {
            await File.WriteAllTextAsync(Path.Combine(output, name), content).ConfigureAwait(false);
        }

        return 0;

        void AddHandle(AtsTypeRef type)
        {
            if (type.Category == AtsTypeCategory.Handle)
            {
                handles.TryAdd(type.TypeId, new AtsTypeInfo { AtsTypeId = type.TypeId });
            }
            if (type.ElementType is { } element)
            {
                AddHandle(element);
            }
        }
    }
}
