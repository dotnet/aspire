// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.TestUtilities;
using Aspire.TestUtilities;
using DotNet.Testcontainers.Builders;
using Testcontainers.Oracle;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public sealed class OracleContainerFixture : IAsyncLifetime
{
    private readonly IMessageSink _diagnosticMessageSink;

    public OracleContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public OracleContainerFixture(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public async ValueTask InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Starting Oracle container initialization..."));

            Container = new OracleBuilder()
                .WithPortBinding(1521, true)
                .WithHostname("localhost")
                .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/gvenzl/oracle-xe:21.3.0-slim-faststart")
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilMessageIsLogged("Completed: ALTER DATABASE OPEN")
                ).Build();

            // Add timeout to fail faster and provide better diagnostics
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            try
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Starting Oracle container with image: {ComponentTestConstants.AspireTestContainerRegistry}/gvenzl/oracle-xe:21.3.0-slim-faststart"));
                await Container.StartAsync(cts.Token);
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Oracle container started successfully"));
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Oracle container failed to start within 3 minutes timeout"));

                // Attempt to get container logs for diagnostics
                try
                {
                    var (stdout, stderr) = await Container.GetLogsAsync(ct: CancellationToken.None);
                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Container stdout logs:\n{stdout}"));
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Container stderr logs:\n{stderr}"));
                    }
                }
                catch (Exception logEx)
                {
                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Failed to retrieve container logs: {logEx.Message}"));
                }

                throw new InvalidOperationException(
                    "Oracle container failed to start within the 3-minute timeout. " +
                    "The container did not log the expected startup completion message: 'Completed: ALTER DATABASE OPEN'. " +
                    "This may indicate Docker resource constraints, networking issues, or problems with the container image. " +
                    "Check the container logs above for more details.",
                    new TimeoutException("Container startup timed out after 3 minutes"));
            }
            catch (Exception ex)
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Oracle container failed to start: {ex.Message}"));
                throw;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}

[CollectionDefinition("Oracle Database collection")]
public class DatabaseCollection : ICollectionFixture<OracleContainerFixture>
{

}
