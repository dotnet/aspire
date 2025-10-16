// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestUtilities;

/// <summary>
/// xUnit v3 test pipeline startup class that initializes Verify snapshot testing infrastructure.
/// Test projects should register this with: [assembly: Xunit.v3.TestPipelineStartup(typeof(VerifyTestPipelineStartup))]
/// </summary>
public sealed class VerifyTestPipelineStartup : Xunit.v3.ITestPipelineStartup
{
    public ValueTask StartAsync(Xunit.Sdk.IMessageSink diagnosticMessageSink)
    {
        // Set the directory for all Verify calls in test projects
        var target = PlatformDetection.IsRunningOnHelix
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")!, "Snapshots")
            : "Snapshots";

        // If target contains an absolute path it will use it as is.
        // If it contains a relative path, it will be combined with the project directory.
        DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, target),
                typeName: type.Name,
                methodName: method.Name));

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync()
    {
        return ValueTask.CompletedTask;
    }
}
