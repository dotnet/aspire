// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators;

namespace ConfigurationSchemaGenerator;

[Generator]
public partial class ConfigSchemaGenerator : IIncrementalGenerator
{
    private const string ConfigurationSchemaAttributeName = "Aspire.ConfigurationSchemaAttribute";

    private const string CompilationOutputPath = "build_property.outputpath";
    private const string FileName = "ConfigurationSchema.json";

    private string? _compilationOutputPath;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if LAUNCH_DEBUGGER
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Launch();
        }
#endif

        var genSpec =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(ConfigurationSchemaAttributeName, (node, _) => true, GetConfigurationSchema)
                .Where(static m => m is not null)
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Combine(context.CompilationProvider)
                .Select((tuple, cancellationToken) =>
                {
                    if (tuple.Left.Left is not ConfigSchemaAttributeInfo configSchemaInfo ||
                        tuple.Left.Right is not { } analyzerConfigOptions ||
                        tuple.Right is not CSharpCompilation cSharpCompilation)
                    {
                        return default;
                    }

                    var parser = new Parser(configSchemaInfo, new KnownTypeSymbols(cSharpCompilation));
                    SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(cancellationToken);
                    ImmutableEquatableArray<DiagnosticInfo>? diagnostics = parser.Diagnostics?.ToImmutableEquatableArray();
                    return (spec, diagnostics, analyzerConfigOptions);
                });

        context.RegisterImplementationSourceOutput(genSpec, ReportDiagnosticsAndEmitSource);
    }

    private void ReportDiagnosticsAndEmitSource(SourceProductionContext sourceProductionContext, (SourceGenerationSpec? SourceGenerationSpec, ImmutableEquatableArray<DiagnosticInfo>? Diagnostics, AnalyzerConfigOptionsProvider? AnalyzerConfigOptions) input)
    {
        if (input.Diagnostics is ImmutableEquatableArray<DiagnosticInfo> diagnostics)
        {
            foreach (DiagnosticInfo diagnostic in diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic.CreateDiagnostic());
            }
        }

        if (input.SourceGenerationSpec is SourceGenerationSpec spec)
        {
            var emitter = new ConfigSchemaEmitter(spec);
            var schema = emitter.GenerateSchema();

            var path = GetDefaultSchemaOutputPath(input.AnalyzerConfigOptions.GlobalOptions);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("""You need to set <CompilerVisibleProperty Include="OutputPath"/>""");
            }

#pragma warning disable RS1035 // Do not use APIs banned for analyzers - needed until https://github.com/dotnet/roslyn/issues/57608 is implemented
            File.WriteAllText(Path.Combine(path, FileName), schema, Encoding.UTF8);
#pragma warning restore RS1035
        }
    }

    private static object? GetConfigurationSchema(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        foreach (var attribute in context.Attributes)
        {
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

    // /// <summary>Data about configuration schema directly from the ConfigurationSchemaAttribute.</summary>
    internal sealed record ConfigSchemaAttributeInfo(INamedTypeSymbol?[]? Types, string?[] ConfigurationPaths, string?[] LogCategories);

    private string GetDefaultSchemaOutputPath(AnalyzerConfigOptions options)
    {
        if (_compilationOutputPath is not null)
        {
            return _compilationOutputPath;
        }

        options.TryGetValue(CompilationOutputPath, out _compilationOutputPath);
        return _compilationOutputPath;
    }
}
