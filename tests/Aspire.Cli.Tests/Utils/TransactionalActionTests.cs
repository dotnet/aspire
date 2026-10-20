// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions;
using Aspire.Cli.Utils;
using Microsoft.DotNet.RemoteExecutor;

namespace Aspire.Cli.Tests.Utils;

public class TransactionalActionTests
{
    // TransactionalAction changes a process-wide timeout, so isolate each test in a child process.
    [Fact]
    public void DisableTransactionTimeoutUpperLimit_SetsMaximumTimeoutToZero()
    {
        RemoteExecutor.Invoke(static () =>
        {
            TransactionManager.MaximumTimeout = TimeSpan.FromMinutes(1);

            TransactionalAction.DisableTransactionTimeoutUpperLimit();

            Assert.Equal(TimeSpan.Zero, TransactionManager.MaximumTimeout);

            TransactionManager.MaximumTimeout = TimeSpan.FromMinutes(2);

            TransactionalAction.DisableTransactionTimeoutUpperLimit();

            Assert.Equal(TimeSpan.Zero, TransactionManager.MaximumTimeout);
        }).Dispose();
    }

    [Fact]
    public void Run_CommitsAndReturnsResult()
    {
        RemoteExecutor.Invoke(static () =>
        {
            TransactionManager.MaximumTimeout = TimeSpan.FromMinutes(1);
            var committed = false;
            var rolledBack = false;

            var result = TransactionalAction.Run(
                action: () =>
                {
                    Assert.Equal(TimeSpan.Zero, TransactionManager.MaximumTimeout);
                    Assert.NotNull(Transaction.Current);
                    return 42;
                },
                commit: () => committed = true,
                rollback: () => rolledBack = true);

            Assert.Equal(42, result);
            Assert.True(committed);
            Assert.False(rolledBack);
        }).Dispose();
    }

    [Fact]
    public void Run_RollsBackWhenActionThrows()
    {
        RemoteExecutor.Invoke(static () =>
        {
            var committed = false;
            var rolledBack = false;
            var exception = new InvalidOperationException("Action failed.");

            var actualException = Assert.Throws<InvalidOperationException>(() => TransactionalAction.Run(
                action: () => throw exception,
                commit: () => committed = true,
                rollback: () => rolledBack = true));

            Assert.Same(exception, actualException);
            Assert.False(committed);
            Assert.True(rolledBack);
        }).Dispose();
    }
}
