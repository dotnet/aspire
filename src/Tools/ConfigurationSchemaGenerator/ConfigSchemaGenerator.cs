// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

public partial class ConfigSchemaGenerator
{
    private const string ConfigurationSchemaAttributeName = "Aspire.ConfigurationSchemaAttribute";

    public static void GenerateSchema(string inputAssembly, string[] references, string outputFile)
    {
        var inputReference = CreateMetadataReference(inputAssembly);
        var compilation = CSharpCompilation.Create(
            "ConfigGenerator",
            references: references.Select(CreateMetadataReference)
                .Concat([inputReference]));

        var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(inputReference);
        var configSchemaInfo = GetConfigurationSchema(assemblySymbol);

        if (configSchemaInfo is not null)
        {
            var parser = new Parser(configSchemaInfo, new KnownTypeSymbols(compilation));
            var spec = parser.GetSourceGenerationSpec(CancellationToken.None);

            var emitter = new ConfigSchemaEmitter(spec, compilation);
            var schema = emitter.GenerateSchema();

            File.WriteAllText(outputFile, schema, Encoding.UTF8);
        }
    }

    private static PortableExecutableReference CreateMetadataReference(string path)
    {
        var docPath = Path.ChangeExtension(path, "xml");
        var documentationProvider = XmlDocumentationProvider.CreateFromFile(docPath);

        return MetadataReference.CreateFromFile(path, documentation: documentationProvider);
    }

    private static ConfigSchemaAttributeInfo? GetConfigurationSchema(IAssemblySymbol assembly)
    {
        foreach (var attribute in assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != ConfigurationSchemaAttributeName)
            {
                continue;
            }

            var items = attribute.NamedArguments;
            INamedTypeSymbol?[]? types = null;
            string?[]? configurationPaths = null;
            string?[]? logCategories = null;

            foreach (var item in items)
            {
                if (item.Key == "Types")
                {
                    types = item.Value.Values.Select(v => v.Value as INamedTypeSymbol).ToArray();
                }
                else if (item.Key == "ConfigurationPaths")
                {
                    configurationPaths = item.Value.Values.Select(v => v.Value as string).ToArray();
                }
                else if (item.Key == "LogCategories")
                {
                    logCategories = item.Value.Values.Select(v => v.Value as string).ToArray();
                }
            }

            if (types is null || configurationPaths is null || logCategories is null)
            {
                throw new InvalidOperationException("Ensure Types, ConfigurationPaths, and LogCategories are set.");
            }

            return new ConfigSchemaAttributeInfo(types, configurationPaths, logCategories);
        }

        return null;
    }

    /// <summary>Data about configuration schema directly from the ConfigurationSchemaAttribute.</summary>
    internal sealed record ConfigSchemaAttributeInfo(INamedTypeSymbol[]? Types, string[] ConfigurationPaths, string[] LogCategories);
}
