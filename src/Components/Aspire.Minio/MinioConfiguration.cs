// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Minio;
internal sealed class MinioConfiguration
{
    public string? Endpoint { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public bool HealthChecks { get; set; } = true;

    public bool Tracing { get; set; } = true;

    public bool Metrics { get; set; } = true;
}
