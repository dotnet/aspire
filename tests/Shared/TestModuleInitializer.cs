// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit.Sdk;
using Xunit.v3;

// Note: This file is shared across multiple test projects.
// The VerifyTestPipelineStartup class uses ITestPipelineStartup to ensure
// Verify is properly initialized before any tests run.
// Each test project that includes this file must register it with:
// [assembly: TestPipelineStartup(typeof(VerifyTestPipelineStartup))]
internal sealed class VerifyTestPipelineStartup : ITestPipelineStartup
{
    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
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
