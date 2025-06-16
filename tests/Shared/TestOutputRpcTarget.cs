// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Backchannel;
using Xunit;

namespace Aspire.Hosting.Utils;

public class TestOutputRpcTarget(ITestOutputHelper testOutputHelper) : ICliRpcTarget
{
    public Task SendCommandErrorAsync(string error, CancellationToken cancellationToken)
    {
        testOutputHelper.WriteLine($"[SendCommandErrorAsync] {error}");
        return Task.CompletedTask;
    }

    public Task SendCommandOutputAsync(string output, CancellationToken cancellationToken)
    {
        testOutputHelper.WriteLine($"[SendCommandOutputAsync] {output}");
        return Task.CompletedTask;
    }
}
