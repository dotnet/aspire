// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Native.CodeGeneration;

internal static class NativeProviderTypeScriptProjection
{
    public static async Task GenerateAsync(string manifestPath, string output)
    {
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath).ConfigureAwait(false))!.AsObject();
        var methods = manifest["capabilities"]!.AsArray().Cast<JsonObject>().Where(method => method["lifecyclePhase"] is null).ToArray();
        var types = methods.SelectMany(method => method["parameters"]!.AsArray().Select(parameter => parameter!["type"]!.AsObject())
            .Append(method["returnType"]!.AsObject())).Where(type => type["category"]!.GetValue<string>() == "Handle")
            .Select(type => type["typeId"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).ToArray();
        var coreTypes = types.Where(type => type.StartsWith("Aspire.Hosting.Native.Server/", StringComparison.Ordinal)).ToArray();
        var ownTypes = types.Except(coreTypes, StringComparer.Ordinal).ToArray();
        var code = new StringBuilder("""
            // Generated offline from portable provider metadata. Do not edit.
            import { type AspireClientRpc, type MarshalledHandle, registerHandleWrapper, wrapIfHandle } from './transport.mjs';

            """);
        if (coreTypes.Length > 0)
        {
            code.AppendLine("import type { " + string.Join(", ", coreTypes.Select(Name)) + " } from './aspire.mjs';");
        }
        code.AppendLine("export class IntegrationClient {");
        code.AppendLine("    constructor(private readonly client: AspireClientRpc) {}");
        foreach (var method in methods.Where(method => method["targetTypeId"] is null))
        {
            AppendMethod(method, instance: false);
        }
        code.AppendLine("}");
        foreach (var type in ownTypes)
        {
            var name = Name(type) + "Client";
            code.AppendLine("export class " + name + " {");
            code.AppendLine("    constructor(private readonly handle: MarshalledHandle, private readonly client: AspireClientRpc) {}");
            code.AppendLine("    toJSON(): MarshalledHandle { return this.handle; }");
            foreach (var method in methods.Where(method => method["targetTypeId"]?.GetValue<string>() == type))
            {
                AppendMethod(method, instance: true);
            }
            code.AppendLine("}");
            code.AppendLine("registerHandleWrapper(" + Quote(type) + ", (handle, client) => new " + name + "(handle, client));");
        }
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "provider-client.mts"), code.ToString()).ConfigureAwait(false);

        void AppendMethod(JsonObject method, bool instance)
        {
            var target = method["targetParameterName"]?.GetValue<string>();
            var parameters = method["parameters"]!.AsArray().Cast<JsonObject>()
                .Where(parameter => !instance || parameter["name"]!.GetValue<string>() != target).ToArray();
            var signature = string.Join(", ", parameters.Select(parameter => parameter["name"]!.GetValue<string>() + ": " + TypeName(parameter["type"]!.AsObject())));
            var returnType = TypeName(method["returnType"]!.AsObject());
            code.AppendLine("    async " + method["method"]!.GetValue<string>() + "(" + signature + "): Promise<" + returnType + "> {");
            code.AppendLine("        const result = await this.client.invokeCapability(" + Quote(method["id"]!.GetValue<string>()) + ", {");
            if (instance)
            {
                code.AppendLine("            " + target + ": this.handle,");
            }
            foreach (var parameter in parameters)
            {
                code.AppendLine("            " + parameter["name"]!.GetValue<string>() + ",");
            }
            code.AppendLine("        });");
            if (returnType != "void")
            {
                code.AppendLine("        return wrapIfHandle(result, this.client) as " + returnType + ";");
            }
            code.AppendLine("    }");
        }

        string TypeName(JsonObject type)
        {
            var id = type["typeId"]!.GetValue<string>();
            return type["category"]!.GetValue<string>() == "Handle" ? Name(id) + (ownTypes.Contains(id) ? "Client" : "") :
                id switch { "string" => "string", "bool" => "boolean", "int" or "long" => "number", "void" => "void", _ => throw new NotSupportedException() };
        }
    }

    private static string Name(string id) => id[(id.LastIndexOf('.') + 1)..].Split('/').Last();
    private static string Quote(string value) => JsonSerializer.Serialize(value);
}
