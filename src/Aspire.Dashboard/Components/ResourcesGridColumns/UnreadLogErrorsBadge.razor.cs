// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components;

public partial class UnreadLogErrorsBadge
{
    private static string GetResourceErrorStructuredLogsUrl(ResourceViewModel resource)
    {
        return $"/StructuredLogs/{resource.Uid}?level=error";
    }
}
