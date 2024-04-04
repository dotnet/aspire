// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Polly;
using Polly.Retry;

namespace Aspire.TestProject;

public static class TestUtils
{
    public static ResiliencePipelineBuilder GetDefaultResiliencePipelineBuilder<TException>(Func<OnRetryArguments<object>, ValueTask> onRetry, int overallTimeoutSecs = 90) where TException : Exception
    {
        var optionsOnRetry = new RetryStrategyOptions
        {
            MaxRetryAttempts = 20,
            ShouldHandle = new PredicateBuilder().Handle<TException>(),
            Delay = TimeSpan.FromSeconds(1),
            OnRetry = onRetry
        };
        return new ResiliencePipelineBuilder()
                    .AddTimeout(TimeSpan.FromSeconds(overallTimeoutSecs))
                    .AddRetry(optionsOnRetry);
    }
}
