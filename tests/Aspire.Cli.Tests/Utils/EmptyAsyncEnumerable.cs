// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests.Utils;

/// <summary>
/// A utility class that provides an empty IAsyncEnumerable&lt;T&gt;.
/// </summary>
/// <typeparam name="T">The type of the elements in the empty enumerable.</typeparam>
internal sealed class EmptyAsyncEnumerable<T>
{
    /// <summary>
    /// Gets a singleton instance of an empty <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static readonly IAsyncEnumerable<T> Instance = new EmptyAsyncEnumerableImpl();

    private sealed class EmptyAsyncEnumerableImpl : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new EmptyAsyncEnumerator();
        }

        private sealed class EmptyAsyncEnumerator : IAsyncEnumerator<T>
        {
            public T Current => default!;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(Task.FromResult(false));
        }
    }
}