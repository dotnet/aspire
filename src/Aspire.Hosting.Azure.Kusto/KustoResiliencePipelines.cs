// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Cloud.Platform.Utils;
using Polly;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Provides pre-configured resilience pipelines for Azure Kusto operations.
/// </summary>
internal static class KustoResiliencePipelines
{
    /// <summary>
    /// Gets a resilience pipeline configured to handle non-permanent exceptions.
    /// </summary>
    public static ResiliencePipeline Default { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            ShouldHandle = new PredicateBuilder().Handle<Exception>(e => e is ICloudPlatformException cpe && !cpe.IsPermanent),
        })
        .Build();
}
