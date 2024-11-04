// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ResourceCommandResponseViewModel
{
    public required ResourceCommandResponseKind Kind { get; init; }

    public ResourceCommandResponseActionKind? ActionKind { get; init; }

    public string? Url { get; init; }

    public string? ErrorMessage { get; init; }
}

// Must be kept in sync with ResourceCommandResponseKind in the resource_service.proto file
public enum ResourceCommandResponseKind
{
    Undefined = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
    Action = 4
}

public enum ResourceCommandResponseActionKind
{
    OpenExternal = 0
}
