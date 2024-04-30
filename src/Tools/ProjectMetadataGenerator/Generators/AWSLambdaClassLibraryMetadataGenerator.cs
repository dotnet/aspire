// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ProjectMetadataGenerator.Generators;

internal static class AWSLambdaClassLibraryMetadataGenerator
{
    public static void Run(string projectPath, string metadataTypeName, string assemblyPath, string outputPath,
        string? typeFilter, string? methodFilter)
    {
        var projectNamespace = Path.GetFileName(projectPath).Replace(".csproj", "");
        var assemblyName = Path.GetFileName(assemblyPath).Replace(".dll", "");
        var assemblyOutputPath = Path.GetDirectoryName(assemblyPath)!;
        var typeValidator = CreateTypeValidator(projectNamespace, typeFilter);
        var methodValidator = CreateMethodValidator(methodFilter);

        var compilation = CSharpCompilation.Create("MetadataGenerator",
            references: [CreateMetadataReference(assemblyPath, assemblyName)]);

        var signatures = FindSignatures(compilation, assemblyName, projectNamespace, typeValidator, methodValidator);

        var metadata = signatures.Count == 0
            ? ""
            : Render(projectPath, assemblyOutputPath, metadataTypeName, projectNamespace, signatures);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, metadata);
    }

    private static Func<string, bool> CreateMethodValidator(string? methodFilter)
    {
        if (string.IsNullOrEmpty(methodFilter))
        {
            return _ => true;
        }

        var filters = methodFilter.Split(",")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return filters.Contains;
    }

    private static Func<ITypeSymbol, bool> CreateTypeValidator(string projectNamespace, string? typeFilter)
    {
        if (string.IsNullOrEmpty(typeFilter))
        {
            return _ => true;
        }

        var filters = typeFilter.Split(",")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => $"{projectNamespace}.{x.Trim()}")
            .ToList();

        return type =>
        {
            var name = type.ToString()!;
            return filters.Any(filter => name.StartsWith(filter));
        };
    }

    private static List<LambdaHandlerSignature> FindSignatures(
        CSharpCompilation compilation,
        string assemblyName,
        string projectNamespace,
        Func<ITypeSymbol, bool> typeValidator,
        Func<string, bool> methodNameValidator)
    {
        var types = compilation.SourceModule.ReferencedAssemblySymbols.Where(x => assemblyName == x.Name)
            .SelectMany(symbol =>
            {
                try
                {
                    var namespaceSymbol = projectNamespace.Split('.')
                        .Aggregate(symbol.GlobalNamespace,
                            (nsSymbol, x) => { return nsSymbol.GetNamespaceMembers().Single(y => y.Name.Equals(x)); });

                    return GetClasses(namespaceSymbol, typeValidator);
                }
                catch
                {
                    return [];
                }
            });

        var signatures = new List<LambdaHandlerSignature>();

        foreach (var typeSymbol in types)
        {
            var methods = typeSymbol.GetMembers();
            var names = methods.Select(x => x.Name).ToArray();

            foreach (var symbol in methods)
            {
                // Initial filtering of eligible methods
                if (symbol is not IMethodSymbol
                    {
                        MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public
                    } method || !methodNameValidator.Invoke(method.Name) || method.Parameters.Length is 0 or > 2)
                {
                    continue;
                }

                var inputParameter = method.Parameters.FirstOrDefault();
                var contextParameter = method.Parameters.Skip(1).FirstOrDefault();
                var contextParameterTypeName =
                    $"{contextParameter?.Type.ContainingNamespace}.{contextParameter?.Type.Name}";
                var awsEventBaseType = ResolveEventType(inputParameter);
                var takesLambdaContext = contextParameterTypeName == "Amazon.Lambda.Core.ILambdaContext";

                if (awsEventBaseType == null && !takesLambdaContext)
                {
                    continue;
                }

                if (names.Count(x => x == symbol.Name) > 1)
                {
                    // Overloads
                    continue;
                }

                if (method.IsGenericMethod)
                {
                    // https://docs.aws.amazon.com/lambda/latest/dg/csharp-handler.html#csharp-handler-restrictions
                    continue;
                }

                signatures.Add(new LambdaHandlerSignature
                {
                    AssemblyName = typeSymbol.ContainingAssembly.Name,
                    TypeName = typeSymbol.ToString()!,
                    MethodName = symbol.Name,
                    Traits =
                    [
                        "IsClassLibrary",
                        takesLambdaContext ? "SignatureWithLambdaContext" : "SignatureWithoutLambdaContext",
                        awsEventBaseType ?? "CustomInputType"
                    ]
                });
            }
        }

        return signatures;
    }

    private static string? ResolveEventType(IParameterSymbol? parameter)
    {
        var name = $"{parameter?.Type.ContainingNamespace}.{parameter?.Type.Name}";

        if (s_eventTypes.Contains(name))
        {
            return name;
        }

        var baseTypeName = $"{parameter?.Type.BaseType?.ContainingNamespace.Name}.{parameter?.Type.BaseType?.Name}";

        if (parameter?.Type.BaseType != null && s_eventTypes.Contains(baseTypeName))
        {
            return baseTypeName;
        }

        return null;
    }

    private static IEnumerable<ITypeSymbol> GetClasses(INamespaceSymbol root,
        Func<ITypeSymbol, bool> typeNameValidator)
    {
        foreach (var namespaceOrTypeSymbol in root.GetMembers())
        {
            switch (namespaceOrTypeSymbol)
            {
                case ITypeSymbol typeSymbol:
                    if (typeSymbol is
                        {
                            TypeKind: TypeKind.Class,
                            DeclaredAccessibility: Accessibility.Public,
                            IsAbstract: false,
                        } && typeNameValidator.Invoke(typeSymbol))
                    {
                        yield return typeSymbol;
                    }

                    break;
                case INamespaceSymbol nsSymbol:
                    {
                        foreach (var type in GetClasses(nsSymbol, typeNameValidator))
                        {
                            yield return type;
                        }

                        break;
                    }
            }
        }
    }

    private static string Render(string projectPath, string outputPath, string metadataTypeName,
        string projectNamespace,
        List<LambdaHandlerSignature> signatures)
    {
        var sources = new List<string>();

        var header = """
                     // <auto-generated/>
                     namespace LambdaFunctions;


                     """;
        sources.Add(header);

        foreach (var signature in signatures)
        {
            var handler = $"{signature.AssemblyName}::{signature.TypeName}::{signature.MethodName}";
            var typeName = signature.TypeName[(projectNamespace.Length + 1)..].Replace(".", "_");
            var className = $"{metadataTypeName}_{typeName}_{signature.MethodName}";

            var traitsStr = string.Join(", ", signature.Traits.Select(x => $"\"{x}\""));

            var source = $$""""
                           [global::System.CodeDom.Compiler.GeneratedCode("Aspire.Hosting.AWS.Lambda", null)]
                           [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Generated code.")]
                           [global::System.Diagnostics.DebuggerDisplay("Type = {GetType().Name,nq}, Handler = {Handler}")]
                           public class {{className}} : global::Aspire.Hosting.AWS.Lambda.ILambdaFunctionMetadata
                           {
                               public string ProjectPath => """{{projectPath}}""";
                               public string Handler => "{{handler}}";
                               public string OutputPath => """{{outputPath}}""";
                               public string[] Traits => [{{traitsStr}}];
                           }


                           """";
            sources.Add(source);
        }

        return string.Join("", sources).TrimEnd();
    }

    private static PortableExecutableReference CreateMetadataReference(string path, string assemblyName)
    {
        var doc =
            $"<?xml version=\"1.0\"?><doc><assembly><name>{assemblyName}</name></assembly><members></members></doc>";
        var documentationProvider = XmlDocumentationProvider.CreateFromBytes(Encoding.UTF8.GetBytes(doc));

        return MetadataReference.CreateFromFile(path, documentation: documentationProvider);
    }

    private sealed class LambdaHandlerSignature
    {
        public required string AssemblyName { get; set; }
        public required string TypeName { get; set; }
        public required string MethodName { get; set; }
        public required List<string> Traits { get; set; }
    }

    private static readonly string[] s_eventTypes =
    [
        "Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerRequest",
        "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest",
        "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest",
        "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest",
        "Amazon.Lambda.CloudWatchLogsEvents.CloudWatchLogsEvent",
        "Amazon.Lambda.CognitoEvents.CognitoCreateAuthChallengeEvent",
        "Amazon.Lambda.CognitoEvents.CognitoCustomEmailSenderEvent",
        "Amazon.Lambda.CognitoEvents.CognitoCustomMessageEvent",
        "Amazon.Lambda.CognitoEvents.CognitoCustomSmsSenderEvent",
        "Amazon.Lambda.CognitoEvents.CognitoDefineAuthChallengeEvent",
        "Amazon.Lambda.CognitoEvents.CognitoEvent",
        "Amazon.Lambda.CognitoEvents.CognitoMigrateUserEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPostAuthenticationEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPostConfirmationEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPreAuthenticationEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPreSignupEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent",
        "Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event",
        "Amazon.Lambda.CognitoEvents.CognitoTriggerEvent",
        "Amazon.Lambda.CognitoEvents.CognitoVerifyAuthChallengeEvent",
        "Amazon.Lambda.ConfigEvents.ConfigEvent",
        "Amazon.Lambda.ConnectEvents.ContactFlowEvent",
        "Amazon.Lambda.DynamoDBEvents.DynamoDBEvent",
        "Amazon.Lambda.DynamoDBEvents.DynamoDBTimeWindowEvent",
        "Amazon.Lambda.DynamoDBEvents.StreamsEventResponse",
        "Amazon.Lambda.KafkaEvents.KafkaEvent",
        "Amazon.Lambda.KinesisAnalyticsEvents.KinesisAnalyticsFirehoseInputPreprocessingEvent",
        "Amazon.Lambda.KinesisAnalyticsEvents.KinesisAnalyticsOutputDeliveryEvent",
        "Amazon.Lambda.KinesisAnalyticsEvents.KinesisAnalyticsStreamsInputPreprocessingEvent",
        "Amazon.Lambda.KinesisFirehoseEvents.KinesisFirehoseEvent",
        "Amazon.Lambda.LexEvents.LexEvent",
        "Amazon.Lambda.LexV2Events.LexV2Event",
        "Amazon.Lambda.MQEvents.ActiveMQEvent",
        "Amazon.Lambda.MQEvents.RabbitMQEvent",
        "Amazon.Lambda.S3Events.S3Event",
        "Amazon.Lambda.S3Events.S3ObjectLambdaEvent",
        "Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent",
        "Amazon.Lambda.SNSEvents.SNSEvent",
        "Amazon.Lambda.SQSEvents.SQSEvent"
    ];
}
