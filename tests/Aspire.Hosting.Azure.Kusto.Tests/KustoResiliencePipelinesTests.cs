// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data.Exceptions;

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class KustoResiliencePipelinesTests
{
    [Fact]
    public async Task ShouldRetryOnTemporaryExceptions()
    {
        // Arrange
        var attemptCount = 0;
        ValueTask work(CancellationToken ct)
        {
            attemptCount++;
            throw new KustoRequestThrottledException();
        }

        // Act + Assert
        await Assert.ThrowsAsync<KustoRequestThrottledException>(async () =>
        {
            await AzureKustoEmulatorResiliencePipelines.Default.ExecuteAsync(work, TestContext.Current.CancellationToken);
        });
        Assert.True(attemptCount > 1, "Operation should have been retried");
    }

    [Fact]
    public async Task ShouldNotRetryOnOtherExceptions()
    {
        // Arrange
        var attemptCount = 0;
        ValueTask work(CancellationToken ct)
        {
            attemptCount++;
            throw new InvalidOperationException();
        }

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await AzureKustoEmulatorResiliencePipelines.Default.ExecuteAsync(work, TestContext.Current.CancellationToken);
        });
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ShouldNotRetryOnPermanentExceptions()
    {
        // Arrange
        var attemptCount = 0;
        ValueTask work(CancellationToken ct)
        {
            attemptCount++;
            throw new KustoBadRequestException();
        }

        // Act + Assert
        await Assert.ThrowsAsync<KustoBadRequestException>(async () =>
        {
            await AzureKustoEmulatorResiliencePipelines.Default.ExecuteAsync(work, TestContext.Current.CancellationToken);
        });
        Assert.Equal(1, attemptCount);
    }
}
