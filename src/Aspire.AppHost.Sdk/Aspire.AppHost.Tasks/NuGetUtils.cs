// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.RuntimeModel;

namespace Aspire.Hosting.Sdk;

/*
 *  These utility methods were copied from the sdk repository to mimic the behavior used when selecting the best matching RID
 *  for a given runtime identifier. For more information, please see the original source code at:
 *  https://github.com/dotnet/sdk/blob/e6da8ca6de3ec8f392dc87b8529415e1ef59b7ea/src/Tasks/Microsoft.NET.Build.Tasks/NuGetUtils.NuGet.cs#L76-L109
 */

internal static class NuGetUtils
{
    public static string? GetBestMatchingRid(RuntimeGraph runtimeGraph, string runtimeIdentifier,
            IEnumerable<string> availableRuntimeIdentifiers, out bool wasInGraph)
    {
        return GetBestMatchingRidWithExclusion(runtimeGraph, runtimeIdentifier,
            runtimeIdentifiersToExclude: null,
            availableRuntimeIdentifiers, out wasInGraph);
    }

    public static string? GetBestMatchingRidWithExclusion(RuntimeGraph runtimeGraph, string runtimeIdentifier,
        IEnumerable<string>? runtimeIdentifiersToExclude,
        IEnumerable<string> availableRuntimeIdentifiers, out bool wasInGraph)
    {
        wasInGraph = runtimeGraph.Runtimes.ContainsKey(runtimeIdentifier);

        string? bestMatch = null;

        HashSet<string> availableRids = new(availableRuntimeIdentifiers, StringComparer.Ordinal);
        HashSet<string>? excludedRids = runtimeIdentifiersToExclude switch { null => null, _ => new HashSet<string>(runtimeIdentifiersToExclude, StringComparer.Ordinal) };
        foreach (var candidateRuntimeIdentifier in runtimeGraph.ExpandRuntime(runtimeIdentifier))
        {
            if (bestMatch == null && availableRids.Contains(candidateRuntimeIdentifier))
            {
                bestMatch = candidateRuntimeIdentifier;
            }

            if (excludedRids != null && excludedRids.Contains(candidateRuntimeIdentifier))
            {
                //  Don't treat this as a match
                return null;
            }
        }

        return bestMatch;
    }
}
