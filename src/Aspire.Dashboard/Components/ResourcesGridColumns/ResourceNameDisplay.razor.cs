// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ResourceNameDisplay
{
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    private int GetUnviewedErrorCount(ResourceViewModel resource)
    {
        if (UnviewedErrorCounts is null)
        {
            return 0;
        }

        var application = TelemetryRepository.GetApplication(resource.Uid);
        return application is null ? 0 : UnviewedErrorCounts.GetValueOrDefault(application, 0);
    }

    private static string GetResourceErrorStructuredLogsUrl(ResourceViewModel resource)
    {
        return $"/StructuredLogs/{resource.Uid}?level=error";
    }

    private string FormatLogLinkTitle(int unviewedErrorCount)
    {
        return FormatName(Resource) + Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Loc[nameof(Columns.UnreadLogErrors)], unviewedErrorCount);
    }
}
