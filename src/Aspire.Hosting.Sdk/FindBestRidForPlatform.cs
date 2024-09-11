// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using NuGet.RuntimeModel;
using Task = Microsoft.Build.Utilities.Task;

namespace Aspire.Hosting.Sdk;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
public class FindBestRidForPlatform : Task
{
    [Required]
    public string RuntimeGraphPath { get; set; }

    [Required]
    public ITaskItem[] SupportedRids { get; set; }

    [Required]
    public string NETCoreSdkRuntimeIdentifier { get; set; }

    [Output]
    public string? BestRidForPlatform { get; set; }

    public override bool Execute()
    {
        var supportedRids = SupportedRids
            .Select(item => item.ItemSpec);

        RuntimeGraph graph = JsonRuntimeFormat.ReadRuntimeGraph(RuntimeGraphPath);

        BestRidForPlatform = NuGetUtils.GetBestMatchingRid(graph, NETCoreSdkRuntimeIdentifier, supportedRids, out bool wasInGraph);

        if (!wasInGraph)
        {
            base.Log.LogError("Rid {0} was not found in the runtime graph.", NETCoreSdkRuntimeIdentifier);
            return false;
        }

        return true;
    }
}
#pragma warning restore CS8618
