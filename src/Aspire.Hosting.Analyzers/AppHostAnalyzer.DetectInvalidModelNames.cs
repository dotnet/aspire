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
            var target = operation.Key.Target;
            var token = operation.Key.ModelNameToken;
            var modelName = token.Text.Trim('"');

            if (!ModelName.TryValidateName(target, modelName, out var validationMessage))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.s_resourceMustHaveValidName, token.GetLocation(), target.ToLower(), modelName, validationMessage));
            }
        }
    }
}
