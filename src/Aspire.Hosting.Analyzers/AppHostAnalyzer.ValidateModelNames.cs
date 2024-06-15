// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aspire.Hosting.Analyzers;

public partial class AppHostAnalyzer
{
    private void ValidateModelNames(OperationBlockAnalysisContext context, ConcurrentDictionary<ModelNameOperation, byte> modelNameOperations)
    {
        if (modelNameOperations.IsEmpty)
        {
            return;
        }

        foreach (var operation in modelNameOperations)
        {
            // TODO: Extract the "Target" from the attribute on the parameter and flow to here
            var target = operation.Key.Target;
            var modelName = operation.Key.ModelNameToken.Text;
            if (!ModelName.TryValidateName(target, modelName, out var validationMessage))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.s_resourceMustHaveValidName, operation.Key.ModelNameToken.GetLocation(), target.ToLower(), modelName, validationMessage));
            }
        }
    }
}
