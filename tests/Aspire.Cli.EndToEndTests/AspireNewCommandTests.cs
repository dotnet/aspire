// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for the 'aspire new' command.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class AspireNewCommandTests
{
    [Fact]
    public Task SmokeTest()
    {
        // Placeholder test to verify CI infrastructure works
        return Task.CompletedTask;
    }
}
