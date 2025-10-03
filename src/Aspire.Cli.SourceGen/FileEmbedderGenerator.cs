// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aspire.Cli.SourceGen;

// Roslyn prefers incremental generators; classic interface is deprecated.
/// <summary>
/// Incremental source generator that embeds the contents of additional files into
/// string properties on classes annotated with <c>[GenerateFileAccessor]</c> and
/// individual <c>[EmbedFile]</c> attributes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FileEmbedderGenerator : IIncrementalGenerator
{
    private const string MarkerAttributeName = "GenerateFileAccessorAttribute";
    private const string FileAttributeName = "EmbedFileAttribute";

    /// <summary>
    /// Sets up the incremental pipeline: adds attribute source, discovers candidate classes,
    /// gathers additional files, and emits a partial class with embedded file accessors.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always add the attribute sources.
        context.RegisterPostInitializationOutput(static ctx => AddAttributeSources(ctx));

        // Collect class declarations with any attributes.
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                                   static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Collect();

        // Collect additional files (as text) so we can embed them.
        var additionalFiles = context.AdditionalTextsProvider.Collect();

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations).Combine(additionalFiles);

        context.RegisterSourceOutput(compilationAndClasses, static (spc, tuple) =>
        {
            var ((compilation, classes), additionalTexts) = tuple;
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            var markerAttr = compilation.GetTypeByMetadataName("Aspire.Cli." + MarkerAttributeName);
            var fileAttr = compilation.GetTypeByMetadataName("Aspire.Cli." + FileAttributeName);
            if (markerAttr is null || fileAttr is null)
            {
                return;
            }

            // Build lookup for additional files by file name and by normalized path suffix.
            var additionalLookup = new Dictionary<string, AdditionalText>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var add in additionalTexts)
            {
                if (!additionalLookup.ContainsKey(System.IO.Path.GetFileName(add.Path)))
                {
                    additionalLookup[System.IO.Path.GetFileName(add.Path)] = add;
                }
            }

            foreach (var classDecl in classes)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                if (model.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol)
                {
                    continue;
                }

                if (!typeSymbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, markerAttr)))
                {
                    continue;
                }

                var props = new List<(string propName, string filePath)>();
                foreach (var attr in typeSymbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, fileAttr)))
                {
                    if (attr.ConstructorArguments.Length == 2)
                    {
                        var filePath = attr.ConstructorArguments[0].Value as string;
                        var propName = attr.ConstructorArguments[1].Value as string;
                        if (!string.IsNullOrWhiteSpace(filePath) && !string.IsNullOrWhiteSpace(propName))
                        {
                            props.Add((propName!, filePath!));
                        }
                    }
                }

                if (props.Count == 0)
                {
                    continue;
                }

                var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? "Aspire.Cli" : typeSymbol.ContainingNamespace.ToDisplayString();
                var className = typeSymbol.Name;
                var source = GeneratePartial(ns, className, props, additionalTexts, additionalLookup);
                spc.AddSource($"{className}.FileEmbedder.g.cs", source);
            }
        });
    }

    private static string GeneratePartial(string ns, string className, List<(string propName, string filePath)> props, System.Collections.Immutable.ImmutableArray<AdditionalText> additionalTexts, Dictionary<string, AdditionalText> additionalLookup)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.IO.Compression;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine($"partial class {className}");
        sb.AppendLine("{");
        foreach (var (propName, filePath) in props)
        {
            // Try exact file name first.
            AdditionalText? text = null;
            if (additionalLookup.TryGetValue(System.IO.Path.GetFileName(filePath), out var candidate))
            {
                text = candidate;
            }
            else
            {
                // Fallback: match by path suffix.
                var normalized = filePath.Replace('\\', '/');
                text = additionalTexts.FirstOrDefault(a => a.Path.Replace('\\', '/').EndsWith(normalized, System.StringComparison.OrdinalIgnoreCase));
            }

            if (text is null)
            {
                continue; // file not supplied as AdditionalFile
            }

            var sourceText = text.GetText();
            if (sourceText is null)
            {
                continue;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sourceText.ToString());
            var compressed = Compress(bytes);
            var byteLiteral = ToByteArrayLiteral(compressed);
            sb.AppendLine($"    private static readonly byte[] __{propName}Data = {byteLiteral};");
            sb.AppendLine($"    public static string {propName} => Decompress(__{propName}Data);");
        }

        sb.AppendLine();
        sb.AppendLine("    private static string Decompress(byte[] data)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var ms = new MemoryStream(data);");
        sb.AppendLine("        using var ds = new DeflateStream(ms, CompressionMode.Decompress);");
        sb.AppendLine("        using var outMs = new MemoryStream();");
        sb.AppendLine("        ds.CopyTo(outMs);");
        sb.AppendLine("        return Encoding.UTF8.GetString(outMs.ToArray());");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();

        static string ToByteArrayLiteral(byte[] data)
        {
            var chunks = string.Join(",", data.Select(b => "0x" + b.ToString("X2")));
            return $"new byte[]{{{chunks}}}";
        }

        static byte[] Compress(byte[] data)
        {
            using var outMs = new MemoryStream();
            using (var ds = new DeflateStream(outMs, CompressionLevel.Optimal, leaveOpen: true))
            {
                ds.Write(data, 0, data.Length);
            }
            return outMs.ToArray();
        }
    }

    private static void AddAttributeSources(IncrementalGeneratorPostInitializationContext context)
    {
        const string attrs = """
// <auto-generated/>
#nullable enable
namespace Aspire.Cli;
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class GenerateFileAccessorAttribute : System.Attribute { }
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class EmbedFileAttribute : System.Attribute {
    public EmbedFileAttribute(string path, string propertyName) { Path = path; PropertyName = propertyName; }
    public string Path { get; }
    public string PropertyName { get; }
}
""";
        context.AddSource("FileEmbedderAttributes.g.cs", attrs);
    }

}
