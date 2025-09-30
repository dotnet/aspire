// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data.Exceptions;
using Polly;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Provides pre-configured resilience pipelines for Azure Kusto operations.
/// </summary>
internal static class KustoResiliencePipelines
{
    /// <summary>
    /// Gets a resilience pipeline configured to handle Kusto throttling exceptions with retry logic.
    /// </summary>
    /// <remarks>
    /// This pipeline retries operations that fail with <see cref="KustoRequestThrottledException"/>
    /// up to 3 times with a 2-second delay between attempts.
    /// </remarks>
    public static ResiliencePipeline ThrottleRetry { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            ShouldHandle = new PredicateBuilder().Handle<KustoRequestThrottledException>(),
        })
        .Build();
}
