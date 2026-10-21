// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Aspire.Hosting.Analyzers;

/// <summary>
/// Records resource registration call sites without evaluating application control flow.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ResourceSourceLocationGenerator : IIncrementalGenerator
{
    private const string GeneratedNamespace = "Aspire.Hosting.Generated";
    private const string AnnotationType = "Aspire.Hosting.ApplicationModel.ResourceSourceLocationAnnotation";
    private const string BuilderType = "Aspire.Hosting.ApplicationModel.IResourceBuilder`1";
    private static readonly SymbolDisplayFormat s_typeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // Keep the analyzer loadable by older compiler hosts. Interception is enabled only when the
    // host provides the opaque-location API; do not reproduce its compiler-owned encoding.
    // https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md
    private static readonly MethodInfo? s_getInterceptableLocation = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).GetMethod(
        "GetInterceptableLocation",
        BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: [typeof(SemanticModel), typeof(InvocationExpressionSyntax), typeof(CancellationToken)],
        modifiers: null);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var invocations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is InvocationExpressionSyntax invocation && GetInvokedName(invocation) is { } name
                && name.Identifier.ValueText.StartsWith("Add", StringComparison.Ordinal),
            static (context, cancellationToken) => GetInvocation(context, cancellationToken))
            .Where(static invocation => invocation is not null)
            .Collect();

        context.RegisterSourceOutput(invocations, static (context, invocations) => Generate(context, invocations));
    }

    private static SimpleNameSyntax? GetInvokedName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name,
        MemberBindingExpressionSyntax member => member.Name,
        SimpleNameSyntax name => name,
        _ => null
    };

    private static Invocation? GetInvocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (s_getInterceptableLocation is null
            || context.Node.SyntaxTree.Options is not CSharpParseOptions options
            || !options.Features.Any(feature =>
                feature.Key is "InterceptorsNamespaces" or "InterceptorsPreviewNamespaces"
                && feature.Value.Split(';').Contains(GeneratedNamespace))
            || context.SemanticModel.Compilation.GetTypeByMetadataName(AnnotationType) is null)
        {
            return null;
        }

        // Anonymous and relative compiler trees cannot identify a physical editor document.
        var filePath = context.Node.SyntaxTree.FilePath;
        var root = Path.GetPathRoot(filePath);
        if (string.IsNullOrEmpty(root) || (Path.DirectorySeparatorChar == '\\' && root.Length < 3))
        {
            return null;
        }

        var syntax = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(syntax, cancellationToken).Symbol is not IMethodSymbol symbol)
        {
            return null;
        }

        var method = (symbol.ReducedFrom ?? symbol).OriginalDefinition;
        // Only wrap calls that can be forwarded as ordinary member access without changing dispatch.
        if (method.MethodKind != MethodKind.Ordinary
            || syntax.Expression is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax }
            || (method.IsStatic && method.ContainingType.TypeKind == TypeKind.Interface && (method.IsAbstract || method.IsVirtual)))
        {
            return null;
        }

        // Assembly accessibility does not make file-local types visible from the generated file.
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsFileLocal)
            {
                return null;
            }
        }

        var compilation = context.SemanticModel.Compilation;
        var builderType = compilation.GetTypeByMetadataName(BuilderType);
        if (builderType is null
            || method.ReturnsVoid
            || method.RefKind != RefKind.None
            || method.IsVararg
            || method.Parameters.Any(parameter => parameter.RefKind is not (RefKind.None or RefKind.Ref or RefKind.Out or RefKind.In)
                || parameter.ScopedKind != ScopedKind.None
                || parameter.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)
            || (!method.IsStatic && method.ContainingType.IsValueType)
            || !compilation.IsSymbolAccessibleWithin(method, compilation.Assembly)
            || GetResourceBuilderType(method.ReturnType, builderType) is not { } resourceBuilderType)
        {
            return null;
        }

        var location = s_getInterceptableLocation.Invoke(null, [context.SemanticModel, syntax, cancellationToken]);
        if (location is null)
        {
            return null;
        }

        var locationType = location.GetType();
        var version = (int)locationType.GetProperty("Version")!.GetValue(location)!;
        var data = (string)locationType.GetProperty("Data")!.GetValue(location)!;
        var name = GetInvokedName(syntax)!;
        var position = name.GetLocation().GetLineSpan().StartLinePosition;
        var text = syntax.SyntaxTree.GetText(cancellationToken).ToString();

        return new Invocation(method, resourceBuilderType, version, data, filePath,
            position.Line + 1, position.Character + 1, GetDocumentHash(text));
    }

    private static INamedTypeSymbol? GetResourceBuilderType(ITypeSymbol type, INamedTypeSymbol builderType) =>
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, builderType)
            ? (INamedTypeSymbol)type
            : type.AllInterfaces.FirstOrDefault(@interface =>
                SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, builderType));

    private static string GetDocumentHash(string text)
    {
        // FNV-1a over UTF-8 is shared with the editor and language SDKs, which cannot all use
        // System.IO.Hashing. The fingerprint is for source freshness, not authentication.
        var hash = 14695981039346656037UL;
        foreach (var value in Encoding.UTF8.GetBytes(text.TrimStart('\uFEFF')))
        {
            hash = unchecked((hash ^ value) * 1099511628211UL);
        }

        return "fnv1a64:" + hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<Invocation?> invocations)
    {
        if (invocations.IsEmpty)
        {
            return;
        }

        // The original calls enforce API opt-ins. Their file-scoped suppressions do not
        // cover this generated forwarding code, so warnings are disabled here.
        var source = new StringBuilder("""
            // <auto-generated/>
            #nullable enable
            #pragma warning disable

            namespace System.Runtime.CompilerServices
            {
                [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute : global::System.Attribute
                {
                    public InterceptsLocationAttribute(int version, string data) { }
                }
            }

            namespace Aspire.Hosting.Generated
            {
                internal static class ResourceSourceLocationInterceptors
                {

            """);

        for (var index = 0; index < invocations.Length; index++)
        {
            WriteInterceptor(source, invocations[index]!, index);
        }

        source.AppendLine("    }").AppendLine("}");
        context.AddSource("ResourceSourceLocationInterceptors.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void WriteInterceptor(StringBuilder source, Invocation invocation, int index)
    {
        var method = invocation.Method;
        var typeParameters = GetTypeParameters(method).ToArray();
        var names = typeParameters.Select((parameter, index) => (parameter, name: $"T{index}"))
            .ToDictionary(pair => pair.parameter, pair => pair.name, SymbolEqualityComparer.Default);

        string TypeName(ITypeSymbol type) => string.Concat(type.ToDisplayParts(s_typeFormat)
            .Select(part => part.Symbol is ITypeParameterSymbol parameter && names.TryGetValue(parameter, out var name)
                ? name : part.ToString()));

        var parameterDeclarations = new List<string>();
        var arguments = new List<string>();
        if (!method.IsStatic)
        {
            parameterDeclarations.Add($"this {TypeName(method.ContainingType)} receiver");
        }

        foreach (var parameter in method.Parameters)
        {
            var modifier = parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => ""
            };
            var name = $"arg{parameter.Ordinal}";
            parameterDeclarations.Add($"{(method.IsExtensionMethod && parameter.Ordinal == 0 ? "this " : "")}{modifier}{TypeName(parameter.Type)} {name}");
            arguments.Add(modifier + name);
        }

        var genericDeclaration = typeParameters.Length == 0 ? "" : $"<{string.Join(", ", typeParameters.Select(p => names[p]))}>";
        var genericArguments = method.TypeParameters.IsEmpty ? "" : $"<{string.Join(", ", method.TypeParameters.Select(p => names[p]))}>";
        var target = method.IsStatic ? TypeName(method.ContainingType) : "receiver";
        var methodName = SyntaxFacts.GetKeywordKind(method.Name) == SyntaxKind.None ? method.Name : "@" + method.Name;
        source.Append("        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
            .Append(invocation.Version).Append(", ").Append(SymbolDisplay.FormatLiteral(invocation.Data, true)).AppendLine(")]");
        source.Append("        public static ").Append(TypeName(method.ReturnType)).Append(" Register").Append(index)
            .Append(genericDeclaration).Append('(').Append(string.Join(", ", parameterDeclarations)).AppendLine(")");

        foreach (var parameter in typeParameters)
        {
            var constraints = new List<string>();
            if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }
            else if (parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(TypeName));
            if (parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                source.Append("            where ").Append(names[parameter]).Append(" : ").AppendLine(string.Join(", ", constraints));
            }
        }

        source.AppendLine("        {")
            .Append("            var result = ").Append(target).Append('.').Append(methodName).Append(genericArguments)
            .Append('(').Append(string.Join(", ", arguments)).AppendLine(");");
        if (method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            source.AppendLine("            if (result is null)")
                .AppendLine("            {")
                .AppendLine("                return result;")
                .AppendLine("            }");
        }
        source.Append("            global::Aspire.Hosting.ApplicationModel.IResource resource = ((").Append(TypeName(invocation.ResourceBuilderType.WithNullableAnnotation(NullableAnnotation.NotAnnotated))).AppendLine(")result).Resource;");

        // Add-style configuration methods can return an existing, untracked builder.
        // Returning the receiver is not evidence that this call created its resource.
        var builderDefinition = invocation.ResourceBuilderType.OriginalDefinition;
        if (!method.IsStatic && GetResourceBuilderType(method.ContainingType, builderDefinition) is { } receiverBuilder)
        {
            WriteExistingResourceGuard("receiver", receiverBuilder, method.ContainingType.NullableAnnotation);
        }
        foreach (var parameter in method.Parameters)
        {
            if (parameter.RefKind != RefKind.Out && GetResourceBuilderType(parameter.Type, builderDefinition) is { } parameterBuilder)
            {
                WriteExistingResourceGuard($"arg{parameter.Ordinal}", parameterBuilder, parameter.Type.NullableAnnotation);
            }
        }

        source.AppendLine("            lock (resource.Annotations)")
            .AppendLine("            {")
            .AppendLine("                if (global::Aspire.Hosting.ApplicationModel.ResourceExtensions.HasAnnotationOfType<global::Aspire.Hosting.ApplicationModel.ResourceSourceLocationAnnotation>(resource))")
            .AppendLine("                {")
            .AppendLine("                    return result;")
            .AppendLine("                }")
            .AppendLine("                resource.Annotations.Add(")
            .Append("                    new global::Aspire.Hosting.ApplicationModel.ResourceSourceLocationAnnotation(")
            .Append(SymbolDisplay.FormatLiteral(invocation.FilePath, true)).Append(", ")
            .Append(invocation.LineNumber).Append(", ").Append(invocation.ColumnNumber).Append(", ")
            .Append(SymbolDisplay.FormatLiteral(invocation.DocumentHash, true)).Append(") { MethodName = ")
            .Append(SymbolDisplay.FormatLiteral(method.Name, true)).AppendLine(" });")
            .AppendLine("            }")
            .AppendLine("            return result;")
            .AppendLine("        }")
            .AppendLine();

        void WriteExistingResourceGuard(string name, INamedTypeSymbol builder, NullableAnnotation nullableAnnotation)
        {
            var indent = "            ";
            if (nullableAnnotation == NullableAnnotation.Annotated)
            {
                source.Append(indent).Append("if (").Append(name).AppendLine(" is not null)")
                    .Append(indent).AppendLine("{");
                indent += "    ";
            }

            source.Append(indent).Append("if (global::System.Object.ReferenceEquals(result, ").Append(name).AppendLine("))")
                .Append(indent).AppendLine("{")
                .Append(indent).AppendLine("    return result;")
                .Append(indent).AppendLine("}")
                .Append(indent).Append("global::Aspire.Hosting.ApplicationModel.IResource ").Append(name).Append("Resource = ((")
                .Append(TypeName(builder.WithNullableAnnotation(NullableAnnotation.NotAnnotated))).Append(')').Append(name).AppendLine(").Resource;");

            // Boxed copies of struct resources retain annotation ownership, not reference identity.
            source.Append(indent).Append("if (global::System.Object.ReferenceEquals(resource, ").Append(name).AppendLine("Resource)")
                .Append(indent).Append("    || (resource.GetType().IsValueType && global::System.Object.ReferenceEquals(resource.Annotations, ")
                .Append(name).AppendLine("Resource.Annotations)))")
                .Append(indent).AppendLine("{")
                .Append(indent).AppendLine("    return result;")
                .Append(indent).AppendLine("}");
            if (nullableAnnotation == NullableAnnotation.Annotated)
            {
                source.AppendLine("            }");
            }
        }
    }

    private static IEnumerable<ITypeParameterSymbol> GetTypeParameters(IMethodSymbol method)
    {
        var types = new Stack<INamedTypeSymbol>();
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            types.Push(type);
        }

        foreach (var type in types)
        {
            foreach (var parameter in type.TypeParameters)
            {
                yield return parameter;
            }
        }

        foreach (var parameter in method.TypeParameters)
        {
            yield return parameter;
        }
    }

    private sealed class Invocation(IMethodSymbol method, INamedTypeSymbol resourceBuilderType, int version, string data, string filePath,
        int lineNumber, int columnNumber, string documentHash)
    {
        public IMethodSymbol Method { get; } = method;
        public INamedTypeSymbol ResourceBuilderType { get; } = resourceBuilderType;
        public int Version { get; } = version;
        public string Data { get; } = data;
        public string FilePath { get; } = filePath;
        public int LineNumber { get; } = lineNumber;
        public int ColumnNumber { get; } = columnNumber;
        public string DocumentHash { get; } = documentHash;
    }
}
