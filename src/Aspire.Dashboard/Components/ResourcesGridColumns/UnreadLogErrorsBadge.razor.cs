// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class UnreadLogErrorsBadge
{
    private int _unviewedCount;

    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }
    [Parameter, EditorRequired]
    public required Dictionary<OtlpApplication, int>? UnviewedErrorCounts { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected override void OnParametersSet()
    {
        _unviewedCount = GetUnviewedErrorCount(Resource);
    }

    private int GetUnviewedErrorCount(ResourceViewModel resource)
    {
        if (UnviewedErrorCounts is null)
        {
            return 0;
        }

        var application = TelemetryRepository.GetApplication(resource.Uid);
        if (application is null)
        {
            return 0;
        }

        if (!UnviewedErrorCounts.TryGetValue(application, out var count))
        {
            return 0;
        }

        return count;
    }

    private static string GetResourceErrorStructuredLogsUrl(ResourceViewModel resource)
    {
        return $"/StructuredLogs/{resource.Uid}?level=error";
    }
}
