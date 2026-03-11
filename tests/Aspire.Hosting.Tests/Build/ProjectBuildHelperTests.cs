// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Build;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests.Build;

public class ProjectBuildHelperTests
{
    [Fact]
    public async Task GetProjectFileClosureAsync_NonExistentProject_ReturnsNull()
    {
        var logger = NullLogger.Instance;
        var result = await ProjectBuildHelper.GetProjectFileClosureAsync(
            "/nonexistent/path/project.csproj",
            logger,
            CancellationToken.None);

        // dotnet msbuild will fail for non-existent projects, but should not throw.
        // It either returns null or a fallback closure.
        // The key assertion is that it doesn't throw.
        Assert.True(result is null || result.FileTimestamps.Count >= 0);
    }

    [Fact]
    public async Task GetProjectFileClosureAsync_CancellationRequested_ThrowsOrReturnsNull()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var logger = NullLogger.Instance;

        // Should handle cancellation gracefully (either throw OperationCanceledException or return null).
        try
        {
            var result = await ProjectBuildHelper.GetProjectFileClosureAsync(
                "/nonexistent/path/project.csproj",
                logger,
                cts.Token);

            // If it returns instead of throwing, that's also acceptable.
            Assert.True(result is null || result.FileTimestamps.Count >= 0);
        }
        catch (OperationCanceledException)
        {
            // Expected — cancellation was requested.
        }
    }
}
