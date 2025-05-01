// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Aspire.Hosting.Sdk;
using NuGet.RuntimeModel;

namespace Aspire.RuntimeIdentifier.Tool;

/// <summary>
/// This task uses the given RID graph in a given SDK to pick the best match from among a set of supported RIDs for the current RID
/// </summary>
public sealed class PickBestRid : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The path to the RID graph to read
    /// </summary>
    [Required]
    public string RuntimeGraphPath { get; set; }

    /// <summary>
    /// The RID of the current process
    /// </summary>
    [Required]
    public string CurrentRid { get; set; }

    /// <summary>
    /// All of the RIDs that Aspire supports
    /// </summary>
    [Required]
    public string[]? SupportedRids { get; set; }

    /// <summary>
    /// The solution to the puzzle
    /// </summary>
    [Output]
    public string MatchingRid { get; set; }

    /// <summary>
    /// Computes the thing
    /// </summary>
    /// <returns></returns>
    public override bool Execute()
    {
        if (!File.Exists(RuntimeGraphPath))
        {
            Log.LogError("File {0} does not exist. Please ensure the runtime graph path exists.", RuntimeGraphPath);
            return !Log.HasLoggedErrors;
        }

        RuntimeGraph graph = JsonRuntimeFormat.ReadRuntimeGraph(RuntimeGraphPath);        
        string? bestRidForPlatform = NuGetUtils.GetBestMatchingRid(graph, CurrentRid, SupportedRids, out bool wasInGraph);

        if (!wasInGraph)
        {
            Log.LogError("Unable to find a matching RID for {0} from among {1} in graph {2}", CurrentRid, string.Join(",", SupportedRids), RuntimeGraphPath);
            return !Log.HasLoggedErrors;
        }

        MatchingRid = bestRidForPlatform;
        return true;
    }
}
