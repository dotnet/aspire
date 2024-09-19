// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Aspire.Hosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class AppHostAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

        // We want ConcurrentHashSet here in case RegisterOperationAction runs in parallel.
        // Since ConcurrentHashSet doesn't exist, use ConcurrentDictionary and ignore the value.
        var concurrentQueue = new ConcurrentQueue<ConcurrentDictionary<ModelNameOperation, byte>>();
        context.RegisterOperationBlockStartAction(context =>
        {
            // Pool and reuse lists for each block.
            if (!concurrentQueue.TryDequeue(out var modelNameOperations))
            {
                modelNameOperations = new ConcurrentDictionary<ModelNameOperation, byte>();
            }

            context.RegisterOperationAction(c => DoOperationAnalysis(c, modelNameOperations), OperationKind.Invocation);

            context.RegisterOperationBlockEndAction(c =>
            {
                DetectInvalidModelNames(c, modelNameOperations);

                // Return to the pool.
                modelNameOperations.Clear();
                concurrentQueue.Enqueue(modelNameOperations);
            });
        });

        void DoOperationAnalysis(OperationAnalysisContext context, ConcurrentDictionary<ModelNameOperation, byte> modelNameOperations)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;

            if (!IsModelNameInvocation(wellKnownTypes, targetMethod, out var parameterData))
            {
                return;
            }

            if (!TryGetStringToken(invocation, parameterData.Value.ModelNameParameter, out var token))
            {
                return;
            }

            modelNameOperations.TryAdd(ModelNameOperation.Create(invocation, parameterData.Value.ModelType, token), value: default);
        }
    }

    private static bool IsModelNameInvocation(WellKnownTypes wellKnownTypes, IMethodSymbol targetMethod, [NotNullWhen(true)] out (IParameterSymbol ModelNameParameter, ModelType ModelType)? parameterData)
    {
        // Look for first string parameter annotated with attribute that implements IModelNameParameter
        ModelType modelType = default;
        var candidateParameter = targetMethod.Parameters.FirstOrDefault(ps =>
            SymbolEqualityComparer.Default.Equals(ps.Type, wellKnownTypes.Get(SpecialType.System_String))
            && HasModelNameAttribute(ps, out modelType));

        if (candidateParameter is not null)
        {
            parameterData = (candidateParameter, modelType);
            return true;
        }

        parameterData = null;
        return false;

        bool HasModelNameAttribute(IParameterSymbol parameter, out ModelType modelType)
        {
            var modelNameParameter = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Aspire_Hosting_ApplicationModel_IModelNameParameter);
            var resourceNameAttribute = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Aspire_Hosting_ApplicationModel_ResourceNameAttribute);

            var attrData = parameter.GetAttributes().SingleOrDefault(a => WellKnownTypes.Implements(a.AttributeClass, modelNameParameter));

            // Model type (e.g. Resource, Endpoint) is based on the concrete attribute type
            modelType = SymbolEqualityComparer.Default.Equals(attrData?.AttributeClass, resourceNameAttribute)
                ? ModelType.Resource
                : ModelType.Unknown;

            return attrData is not null;
        }
    }

    private static bool TryGetStringToken(IInvocationOperation invocation, IParameterSymbol modelNameParameter, out SyntaxToken token)
    {
        IArgumentOperation? argumentOperation = null;
        foreach (var argument in invocation.Arguments)
        {
            if (SymbolEqualityComparer.Default.Equals(modelNameParameter, argument.Parameter))
            {
                argumentOperation = argument;
                break;
            }
        }

        if (argumentOperation?.Syntax is not ArgumentSyntax routePatternArgumentSyntax ||
            routePatternArgumentSyntax.Expression is not LiteralExpressionSyntax routePatternArgumentLiteralSyntax)
        {
            token = default;
            return false;
        }

        token = routePatternArgumentLiteralSyntax.Token;
        return true;
    }

    private record struct ModelNameOperation(IInvocationOperation Operation, ModelType ModelType, SyntaxToken ModelNameToken)
    {
        public static ModelNameOperation Create(IInvocationOperation operation, ModelType modelType, SyntaxToken modelNameToken)
        {
            return new ModelNameOperation(operation, modelType, modelNameToken);
        }
    }

    private enum ModelType
    {
        Unknown,
        Resource
    }
}
