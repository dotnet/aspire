// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aspire.Hosting.Analyzers;

public partial class AppHostAnalyzer
{
    private void DetectInvalidModelNames(OperationBlockAnalysisContext context, ConcurrentDictionary<ModelNameOperation, byte> modelNameOperations)
    {
        if (modelNameOperations.IsEmpty)
        {
            return;
        }

        foreach (var operation in modelNameOperations)
        {
            var modelTypes = operation.Key.ModelTypes;
            var token = operation.Key.ModelNameToken;
            var modelName = token.Value?.ToString();

            if (modelName is not null && modelTypes.Length > 0)
            {
                foreach (var modelType in modelTypes)
                {
                    if (!ModelName.TryValidateName(modelType.ToString(), modelName, out var validationMessage))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.s_modelNameMustBeValid, token.GetLocation(), validationMessage));
                    }
                }
            }
        }
    }
}
