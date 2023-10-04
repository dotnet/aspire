// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Publishing;

public sealed class PublishingOptions
{
    public const string Publishing = "Publishing";

    public string? Publisher { get; set; }
    public string? OutputPath { get; set; }
}
