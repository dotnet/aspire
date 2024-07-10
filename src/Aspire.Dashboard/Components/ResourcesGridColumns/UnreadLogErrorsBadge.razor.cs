// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class UnreadLogErrorsBadge
{
    private string? _applicationName;
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
        (_applicationName, _unviewedCount) = GetUnviewedErrorCount(Resource);
    }

    private (string? applicationName, int unviewedErrorCount) GetUnviewedErrorCount(ResourceViewModel resource)
    {
        if (UnviewedErrorCounts is null)
        {
            return (null, 0);
        }

        var application = TelemetryRepository.GetApplicationByCompositeName(resource.Name);
        if (application is null)
        {
            return (null, 0);
        }

        if (!UnviewedErrorCounts.TryGetValue(application, out var count) || count == 0)
        {
            return (null, 0);
        }

        var applications = TelemetryRepository.GetApplications();
        var applicationName = applications.Count(a => a.ApplicationName == application.ApplicationName) > 1
            ? application.InstanceId
            : application.ApplicationName;

        return (applicationName, count);
    }

    private string GetResourceErrorStructuredLogsUrl()
    {
        return DashboardUrls.StructuredLogsUrl(resource: _applicationName, logLevel: "error");
    }
}
