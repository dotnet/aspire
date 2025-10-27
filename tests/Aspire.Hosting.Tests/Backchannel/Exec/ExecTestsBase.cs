// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.Backchannel.Exec;

public abstract class ExecTestsBase(ITestOutputHelper outputHelper)
{
    protected readonly ITestOutputHelper _outputHelper = outputHelper;

    /// <summary>
    /// Performs an `exec` against the apphost,
    /// collecting the logs of the `exec` resource apphost is being run against.
    ///
    /// Also awaits the app startup. It has to be built before running this method.
    /// </summary>
    internal async Task<List<CommandOutput>> ExecWithLogCollectionAsync(
        DistributedApplication app,
        int timeoutSec = 30)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));

        var appHostRpcTarget = app.Services.GetRequiredService<AppHostRpcTarget>();
        var outputStream = appHostRpcTarget.ExecAsync(cts.Token);

        var logs = new List<CommandOutput>();
        var startTask = app.StartAsync(cts.Token);
        await foreach (var message in outputStream)
        {
            var logLevel = message.IsErrorMessage ? "error" : "info";
            var log = $"Received output: #{message.LineNumber} [level={logLevel}] [type={message.Type}] {message.Text}";

            logs.Add(message);
            _outputHelper.WriteLine(log);
        }

        await startTask;
        return logs;
    }

    internal static void AssertLogsContain(List<CommandOutput> logs, params string[] expectedLogMessages)
    {
        if (expectedLogMessages.Length == 0)
        {
            Assert.Empty(logs);
            return;
        }

        foreach (var expectedMessage in expectedLogMessages)
        {
            var logFound = logs.Any(x => x.Text.Contains(expectedMessage));
            Assert.True(logFound, $"Expected log message '{expectedMessage}' not found in logs.");
        }
    }

    protected IDistributedApplicationTestingBuilder PrepareBuilder(string[] args)
    {
        var builder = TestDistributedApplicationBuilder.Create(_outputHelper, args).WithTestAndResourceLogging(_outputHelper);
        builder.Configuration[KnownConfigNames.UnixSocketPath] = UnixSocketHelper.GetBackchannelSocketPath();
        return builder;
    }
}
