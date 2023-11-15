// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dapr;

public sealed record DaprComponentOptions
{
    public string? LocalPath { get; init; }
}
