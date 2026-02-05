// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.DebugAdapter.Generator.Schema;
using Microsoft.CodeAnalysis;

namespace Aspire.DebugAdapter.Generator;

/// <summary>
/// Incremental source generator that emits DAP protocol types.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DebugAdapterSourceGenerator : IIncrementalGenerator
{
    private const string DefaultNamespace = "Aspire.DebugAdapter.Types";
    private const string NamespacePropertyName = "build_property.DebugAdapterTypesNamespace";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get the namespace from MSBuild properties
        var namespaceProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue(NamespacePropertyName, out var ns);
                return string.IsNullOrWhiteSpace(ns) ? DefaultNamespace : ns;
            });

        // Register output - schema is static, so we use RegisterPostInitializationOutput
        // But we need the namespace, so we combine with the namespace provider
        context.RegisterSourceOutput(namespaceProvider, (ctx, ns) =>
        {
            try
            {
                var schemaJson = LoadEmbeddedSchema();
                var schema = JsonSerializer.Deserialize<JsonSchemaDocument>(schemaJson);

                if (schema is null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASPIREDAP001",
                            "Failed to parse schema",
                            "Failed to deserialize the DAP JSON schema",
                            "Aspire.DebugAdapter",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                    return;
                }

                var parser = new SchemaParser(schema);
                var types = parser.Parse();

                var generator = new CSharpGenerator(types, ns);

                foreach (var (hintName, source) in generator.Generate())
                {
                    ctx.AddSource(hintName, source);
                }
            }
            catch (Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASPIREDAP002",
                        "Generation failed",
                        "DAP code generation failed: {0}",
                        "Aspire.DebugAdapter",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    ex.Message));
            }
        });
    }

    private static string LoadEmbeddedSchema()
    {
        var assembly = typeof(DebugAdapterSourceGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream("dapschema.json");

        if (stream is null)
        {
            throw new InvalidOperationException(
                "Embedded resource 'dapschema.json' not found. Available resources: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
