// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on dotnet/sdk src/Cli/dotnet/TransactionalAction.cs

using System.Transactions;

namespace Aspire.Cli.Utils;

/// <summary>
/// Provides transactional file operations with automatic rollback on failure.
/// Based on patterns from dotnet/sdk.
/// </summary>
public sealed class TransactionalAction
{
    static TransactionalAction()
    {
        DisableTransactionTimeoutUpperLimit();
    }

    private class EnlistmentNotification(Action? commit, Action? rollback) : IEnlistmentNotification
    {
        private Action? _commit = commit;
        private Action? _rollback = rollback;

        public void Commit(Enlistment enlistment)
        {
            if (_commit != null)
            {
                _commit();
                _commit = null;
            }

            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment enlistment)
        {
            enlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            if (_rollback != null)
            {
                _rollback();
                _rollback = null;
            }

            enlistment.Done();
        }
    }

    /// <summary>
    /// Runs an action with transactional semantics.
    /// If the action throws, the rollback action is executed.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">The action to perform.</param>
    /// <param name="commit">Optional action to run on successful commit.</param>
    /// <param name="rollback">Optional action to run on rollback.</param>
    /// <returns>The result of the action.</returns>
    public static T Run<T>(
        Func<T> action,
        Action? commit = null,
        Action? rollback = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        // This automatically inherits any ambient transaction
        // If a transaction is inherited, completing this scope will be a no-op
        T result = default!;
        try
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                TimeSpan.Zero);

            Transaction.Current!.EnlistVolatile(
                new EnlistmentNotification(commit, rollback),
                EnlistmentOptions.None);

            result = action();

            scope.Complete();

            return result;
        }
        catch (TransactionAbortedException)
        {
            throw;
        }
    }

    /// <summary>
    /// Disables the transaction timeout upper limit.
    /// </summary>
    public static void DisableTransactionTimeoutUpperLimit()
    {
        TransactionManager.MaximumTimeout = TimeSpan.Zero;
    }

    /// <summary>
    /// Runs an action with transactional semantics.
    /// If the action throws, the rollback action is executed.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="commit">Optional action to run on successful commit.</param>
    /// <param name="rollback">Optional action to run on rollback.</param>
    public static void Run(
        Action action,
        Action? commit = null,
        Action? rollback = null)
    {
        Run<object?>(
            action: () =>
            {
                action();
                return null;
            },
            commit: commit,
            rollback: rollback);
    }
}
