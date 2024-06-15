// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aspire.Hosting.Analyzers;

/// <summary>
/// 
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class AppHostAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// 
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.SupportedDiagnostics;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // TODO: Don't register the analyzer if the project has disabled the analyzer in the project file.

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
    }

    private void AnalyzeParameter(SymbolAnalysisContext context)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        var isStringType = parameter.Type.SpecialType is SpecialType.System_String;

        if (!isStringType || !parameter.HasExplicitDefaultValue)
        {
            return;
        }

        var modelNameAttribute = context.Compilation.GetTypeByMetadataName("Aspire.Hosting.ApplicationModel.ModelNameAttribute");
        var hasModelNameAttribute = parameter.GetAttributes().Any(a => a.AttributeClass?.Equals(modelNameAttribute, SymbolEqualityComparer.Default) == true);

        if (!hasModelNameAttribute)
        {
            return;
        }

        var value = parameter.ExplicitDefaultValue;
        if (value is string modelName && !ModelName.TryValidateName(parameter.Name, modelName, out var validationMessage))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.s_resourceMustHaveValidName, context.Symbol.Locations.FirstOrDefault(), context.Symbol.Locations, validationMessage));
        }
    }
}
