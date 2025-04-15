// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.ConsoleLogs;
using Aspire.Hosting.Dcp;

namespace Aspire.Hosting.Tests.Utils;

internal sealed class TestDcpExecutor : IDcpExecutor
{
    private readonly Channel<IReadOnlyList<LogEntry>>? _getConsoleLogsChannel;

    public TestDcpExecutor(Channel<IReadOnlyList<LogEntry>>? getConsoleLogsChannel = null)
    {
        _getConsoleLogsChannel = getConsoleLogsChannel;
    }

    public async IAsyncEnumerable<IReadOnlyList<LogEntry>> GetConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_getConsoleLogsChannel == null)
        {
            throw new InvalidOperationException("No console logs writer.");
        }

        await foreach (var result in _getConsoleLogsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }
    }

    public IResourceReference GetResource(string resourceName) => throw new NotImplementedException();

    public Task RunApplicationAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken) => Task.CompletedTask;
}
