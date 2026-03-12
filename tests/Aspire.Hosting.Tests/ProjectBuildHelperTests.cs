// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests;

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
        // It should return null when the project doesn't exist.
        Assert.Null(result);
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

            // If it returns instead of throwing, it should be null for a non-existent project.
            Assert.Null(result);
        }
        catch (OperationCanceledException)
        {
            // Expected — cancellation was requested.
        }
    }
}
