// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Cloud.Platform.Utils;
using Polly;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Provides pre-configured resilience pipelines for Azure Kusto emulator operations.
/// </summary>
internal static class AzureKustoEmulatorResiliencePipelines
{
    /// <summary>
    /// Gets a resilience pipeline configured to handle non-permanent exceptions.
    /// </summary>
    public static ResiliencePipeline Default { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(IsTransient),
        })
        .Build();

    private static bool IsTransient(Exception ex) => ex is ICloudPlatformException cpe && !cpe.IsPermanent;
}
