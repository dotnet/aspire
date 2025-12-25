// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aspire.Hosting.CodeGeneration.Models;

namespace Aspire.Hosting.CodeGeneration.TypeScript;

/// <summary>
/// Generates TypeScript code from the Aspire application model.
/// </summary>
public sealed partial class TypeScriptCodeGenerator : ICodeGenerator
{
    /// <inheritdoc />
    public string Language => "TypeScript";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(ApplicationModel model)
    {
        var files = new Dictionary<string, string>();

        // Generate main index.ts that re-exports everything
        var indexBuilder = new StringBuilder();
        indexBuilder.AppendLine("// Auto-generated Aspire TypeScript SDK");
        indexBuilder.AppendLine("// Do not edit manually");
        indexBuilder.AppendLine();
        indexBuilder.AppendLine("export * from './distributed-application.js';");
        indexBuilder.AppendLine("export * from './types.js';");
        indexBuilder.AppendLine("export * from './client.js';");

        foreach (var integration in model.Integrations)
        {
            var moduleName = GetModuleName(integration.PackageId);
            indexBuilder.AppendLine(CultureInfo.InvariantCulture, $"export * from './integrations/{moduleName}.js';");
        }

        files["index.ts"] = indexBuilder.ToString();

        // Generate types.ts with common type definitions
        files["types.ts"] = GenerateTypesFile();

        // Generate each integration file
        foreach (var integration in model.Integrations)
        {
            var (path, content) = GenerateIntegrationFile(integration);
            files[path] = content;
        }

        return files;
    }

    /// <inheritdoc />
    public Dictionary<string, string> GenerateIntegration(IntegrationModel integration)
    {
        var files = new Dictionary<string, string>();
        var moduleName = GetModuleName(integration.PackageId);
        var content = GenerateIntegrationContent(integration);
        files[string.Create(CultureInfo.InvariantCulture, $"integrations/{moduleName}.ts")] = content;
        return files;
    }

    /// <inheritdoc />
    public Dictionary<string, string> GenerateResource(ResourceModel resource)
    {
        var files = new Dictionary<string, string>();
        var content = GenerateResourceClass(resource);
        var fileName = ToKebabCase(resource.TypeName);
        files[string.Create(CultureInfo.InvariantCulture, $"resources/{fileName}.ts")] = content;
        return files;
    }

    private static string GenerateTypesFile()
    {
        var builder = new StringBuilder();
        builder.AppendLine("// Auto-generated type definitions");
        builder.AppendLine();

        // Instruction types
        builder.AppendLine("export interface InstructionResult {");
        builder.AppendLine("  success: boolean;");
        builder.AppendLine("  builderName?: string;");
        builder.AppendLine("  resourceName?: string;");
        builder.AppendLine("  result?: unknown;");
        builder.AppendLine("  error?: string;");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("export interface CreateBuilderInstruction {");
        builder.AppendLine("  name: 'CREATE_BUILDER';");
        builder.AppendLine("  builderName: string;");
        builder.AppendLine("  args?: string[];");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("export interface InvokeInstruction {");
        builder.AppendLine("  name: 'INVOKE';");
        builder.AppendLine("  builderName: string;");
        builder.AppendLine("  resourceName?: string;");
        builder.AppendLine("  methodName: string;");
        builder.AppendLine("  args: unknown[];");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("export interface RunBuilderInstruction {");
        builder.AppendLine("  name: 'RUN_BUILDER';");
        builder.AppendLine("  builderName: string;");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("export type AnyInstruction = CreateBuilderInstruction | InvokeInstruction | RunBuilderInstruction;");
        builder.AppendLine();

        // Resource builder options
        builder.AppendLine("export interface ResourceBuilderOptions {");
        builder.AppendLine("  name: string;");
        builder.AppendLine("}");
        builder.AppendLine();

        // Endpoint options
        builder.AppendLine("export interface EndpointOptions {");
        builder.AppendLine("  port?: number;");
        builder.AppendLine("  targetPort?: number;");
        builder.AppendLine("  scheme?: 'http' | 'https' | 'tcp';");
        builder.AppendLine("  name?: string;");
        builder.AppendLine("}");
        builder.AppendLine();

        // Environment variable options
        builder.AppendLine("export interface EnvironmentOptions {");
        builder.AppendLine("  name: string;");
        builder.AppendLine("  value: string | (() => string);");
        builder.AppendLine("}");
        builder.AppendLine();

        // Volume options
        builder.AppendLine("export interface VolumeOptions {");
        builder.AppendLine("  name?: string;");
        builder.AppendLine("  target: string;");
        builder.AppendLine("  isReadOnly?: boolean;");
        builder.AppendLine("}");
        builder.AppendLine();

        // Bind mount options
        builder.AppendLine("export interface BindMountOptions {");
        builder.AppendLine("  source: string;");
        builder.AppendLine("  target: string;");
        builder.AppendLine("  isReadOnly?: boolean;");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static (string path, string content) GenerateIntegrationFile(IntegrationModel integration)
    {
        var moduleName = GetModuleName(integration.PackageId);
        var content = GenerateIntegrationContent(integration);
        return (string.Create(CultureInfo.InvariantCulture, $"integrations/{moduleName}.ts"), content);
    }

    private static string GenerateIntegrationContent(IntegrationModel integration)
    {
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture, $"// Auto-generated from {integration.PackageId} v{integration.Version}");
        builder.AppendLine();
        builder.AppendLine("import type { DistributedApplicationBuilder, ResourceBuilder } from '../distributed-application.js';");
        builder.AppendLine("import type { InstructionResult } from '../types.js';");
        builder.AppendLine();

        // Generate extension function for each method
        foreach (var method in integration.ExtensionMethods)
        {
            if (!method.ExtendedType.Contains("IDistributedApplicationBuilder", StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine(GenerateExtensionFunction(method));
            builder.AppendLine();
        }

        // Generate ResourceBuilder extension methods
        var resourceBuilderMethods = integration.ExtensionMethods
            .Where(m => m.ExtendedType.Contains("IResourceBuilder", StringComparison.Ordinal))
            .ToList();

        if (resourceBuilderMethods.Count > 0)
        {
            builder.AppendLine("// ResourceBuilder extension methods");
            foreach (var method in resourceBuilderMethods)
            {
                builder.AppendLine(GenerateResourceBuilderExtension(method));
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string GenerateExtensionFunction(ExtensionMethodModel method)
    {
        var builder = new StringBuilder();

        // Add JSDoc comment
        if (!string.IsNullOrEmpty(method.Documentation))
        {
            builder.AppendLine("/**");
            builder.AppendLine(CultureInfo.InvariantCulture, $" * {method.Documentation}");
            builder.AppendLine(" */");
        }

        var funcName = ToCamelCase(method.Name);
        var resourceType = method.ResourceType != null ? GetTypeScriptType(method.ResourceType) : "unknown";

        // Build parameters (skip 'this' parameter)
        var parameters = method.Parameters
            .Where(p => !p.IsThis)
            .Select(FormatParameter)
            .ToList();

        var paramList = parameters.Count > 0 ? string.Join(", ", parameters) : "";
        var builderParam = parameters.Count > 0
            ? string.Create(CultureInfo.InvariantCulture, $"builder: DistributedApplicationBuilder, {paramList}")
            : "builder: DistributedApplicationBuilder";

        builder.AppendLine(CultureInfo.InvariantCulture, $"export async function {funcName}({builderParam}): Promise<ResourceBuilder<'{resourceType}'>> {{");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  const result = await builder.invoke('{method.Name}', [{GetArgumentsList(method)}]);");
        builder.AppendLine("  if (!result.success) {");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    throw new Error(result.error || 'Failed to invoke {method.Name}');");
        builder.AppendLine("  }");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  return builder.getResourceBuilder<'{resourceType}'>(result.resourceName!);");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateResourceBuilderExtension(ExtensionMethodModel method)
    {
        var builder = new StringBuilder();
        var funcName = ToCamelCase(method.Name);

        // Extract resource type from IResourceBuilder<T>
        var resourceTypeMatch = ResourceBuilderTypeRegex().Match(method.ExtendedType);
        var resourceType = resourceTypeMatch.Success ? resourceTypeMatch.Groups[1].Value : "unknown";

        // Build parameters (skip 'this' parameter)
        var parameters = method.Parameters
            .Where(p => !p.IsThis)
            .Select(FormatParameter)
            .ToList();

        var paramList = parameters.Count > 0 ? string.Join(", ", parameters) : "";
        var rbParam = parameters.Count > 0
            ? string.Create(CultureInfo.InvariantCulture, $"resourceBuilder: ResourceBuilder<'{resourceType}'>, {paramList}")
            : string.Create(CultureInfo.InvariantCulture, $"resourceBuilder: ResourceBuilder<'{resourceType}'>");

        builder.AppendLine(CultureInfo.InvariantCulture, $"export async function {funcName}({rbParam}): Promise<ResourceBuilder<'{resourceType}'>> {{");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  const result = await resourceBuilder.invoke('{method.Name}', [{GetArgumentsList(method)}]);");
        builder.AppendLine("  if (!result.success) {");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    throw new Error(result.error || 'Failed to invoke {method.Name}');");
        builder.AppendLine("  }");
        builder.AppendLine("  return resourceBuilder;");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateResourceClass(ResourceModel resource)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"// Auto-generated from {resource.PackageId}");
        builder.AppendLine();
        builder.AppendLine("import type { ResourceBuilder } from '../distributed-application.js';");
        builder.AppendLine();

        if (!string.IsNullOrEmpty(resource.Documentation))
        {
            builder.AppendLine("/**");
            builder.AppendLine(CultureInfo.InvariantCulture, $" * {resource.Documentation}");
            builder.AppendLine(" */");
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"export interface {resource.TypeName}Resource {{");

        foreach (var prop in resource.Properties)
        {
            var tsType = GetTypeScriptType(prop.Type);
            builder.AppendLine(CultureInfo.InvariantCulture, $"  readonly {ToCamelCase(prop.Name)}: {tsType};");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string FormatParameter(ParameterModel param)
    {
        var tsType = GetTypeScriptType(param.Type);
        var optional = param.IsOptional ? "?" : "";
        return string.Create(CultureInfo.InvariantCulture, $"{ToCamelCase(param.Name)}{optional}: {tsType}");
    }

    private static string GetArgumentsList(ExtensionMethodModel method)
    {
        var args = method.Parameters
            .Where(p => !p.IsThis)
            .Select(p => ToCamelCase(p.Name));
        return string.Join(", ", args);
    }

    private static string GetTypeScriptType(string dotNetType)
    {
        // Handle common .NET to TypeScript type mappings
        return dotNetType switch
        {
            "string" or "String" => "string",
            "int" or "Int32" or "long" or "Int64" or "short" or "Int16" => "number",
            "float" or "Single" or "double" or "Double" or "decimal" or "Decimal" => "number",
            "bool" or "Boolean" => "boolean",
            "void" or "Void" => "void",
            "object" or "Object" => "unknown",
            _ when dotNetType.EndsWith('?') => string.Create(CultureInfo.InvariantCulture, $"{GetTypeScriptType(dotNetType[..^1])} | undefined"),
            _ when dotNetType.Contains("IEnumerable", StringComparison.Ordinal) || dotNetType.Contains("List", StringComparison.Ordinal) || dotNetType.Contains("[]", StringComparison.Ordinal) => "unknown[]",
            _ when dotNetType.Contains("Dictionary", StringComparison.Ordinal) => "Record<string, unknown>",
            _ when dotNetType.Contains("Action", StringComparison.Ordinal) || dotNetType.Contains("Func", StringComparison.Ordinal) => "(...args: unknown[]) => unknown",
            _ => "unknown"
        };
    }

    private static string GetModuleName(string packageId)
    {
        // Convert "Aspire.Hosting.Redis" to "redis"
        var parts = packageId.Split('.');
        var name = parts.Length > 2 ? parts[^1] : parts[^1];
        return name.ToLowerInvariant();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        return string.Create(CultureInfo.InvariantCulture, $"{char.ToLowerInvariant(name[0])}{name[1..]}");
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    [GeneratedRegex(@"IResourceBuilder<(\w+)>")]
    private static partial Regex ResourceBuilderTypeRegex();
}
