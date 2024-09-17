// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aspire.Hosting.Analyzers;

public partial class AppHostAnalyzer
{
    private void DetectNonUniqueResourceNames(OperationBlockAnalysisContext context, ConcurrentDictionary<ModelNameOperation, byte> modelNameOperations)
    {
        if (modelNameOperations.IsEmpty)
        {
            return;
        }

        // Map of resource names to the operations that define them.
        var resourceOperations = new Dictionary<string, List<ModelNameOperation>>(StringComparers.ResourceName);

        foreach (var operation in modelNameOperations)
        {
            if (operation.Key.Target != "Resource")
            {
                continue;
            }

            var token = operation.Key.ModelNameToken;
            var resourceName = token.Value?.ToString();

            if (resourceOperations.TryGetValue(resourceName, out var existingOperations))
            {
                existingOperations.Add(operation.Key);
            }
            else if (resourceName is not null)
            {
                resourceOperations.Add(resourceName, [operation.Key]);
            }
        }

        foreach (var resourceName in resourceOperations.Keys)
        {
            var operations = resourceOperations[resourceName];
            if (operations.Count > 1)
            {
                foreach (var operation in operations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.s_resourceNameMustBeUnique, operation.ModelNameToken.GetLocation(), resourceName));
                }
            }
        }
    }
}
