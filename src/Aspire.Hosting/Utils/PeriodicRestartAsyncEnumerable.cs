// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// An <see cref="IAsyncEnumerable{T}"/> that periodically restarts an inner enumerable after a timeout.
/// We specifically need this for the DCP resource watch enumerable, as the WatchAsync call can be
/// unreliable when running long enough (30+ minutes). The watch stops receiving new events, but
/// no cancellation (or other exception) is thrown. The simple fix is to periodically restart the watch
/// with the only downside being that we'll receive a duplicate of the latest resource state every time
/// we re-enable the watch.
/// </summary>
/// <typeparam name="T">The inner enumerated type</typeparam>
internal sealed class PeriodicRestartAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Func<CancellationToken, Task<IAsyncEnumerable<T>>> _enumerableFactory;
    private readonly TimeSpan _restartTimeout;

    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> that wraps an inner enumerable factory with periodic restart of the inner enumerable.
    /// </summary>
    /// <param name="enumerableFactory">A factory method that returns a new inner <see cref="IAsyncEnumerable{T}"/></param>
    /// <param name="restartTimeout">How often should the inner enumerable be restarted</param>
    public PeriodicRestartAsyncEnumerable(Func<CancellationToken, Task<IAsyncEnumerable<T>>> enumerableFactory, TimeSpan restartTimeout)
    {
        _enumerableFactory = enumerableFactory;
        _restartTimeout = restartTimeout;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new PeriodicRestartAsyncEnumerator(_enumerableFactory, _restartTimeout, cancellationToken);
    }

    public sealed class PeriodicRestartAsyncEnumerator : IAsyncEnumerator<T>
    {
        private readonly Func<CancellationToken, Task<IAsyncEnumerable<T>>> _enumerableFactory;
        private readonly TimeSpan _restartTimeout;
        private readonly CancellationToken _cancellationToken;
        private IAsyncEnumerator<T>? _innerEnumerator;
        private CancellationTokenSource? _restartCts;

        public PeriodicRestartAsyncEnumerator(Func<CancellationToken, Task<IAsyncEnumerable<T>>> enumerableFactory, TimeSpan restartTimeout, CancellationToken cancellationToken)
        {
            _enumerableFactory = enumerableFactory;
            _restartTimeout = restartTimeout;
            _cancellationToken = cancellationToken;
        }

        public T Current => _innerEnumerator!.Current;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _restartCts, null)?.Dispose();
            if (_innerEnumerator is not null)
            {
                return _innerEnumerator.DisposeAsync();
            }
            else
            {
                return ValueTask.CompletedTask;
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_innerEnumerator == null)
            {
                var newRetryCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                var oldRetryCts = Interlocked.Exchange(ref _restartCts, newRetryCts);
                oldRetryCts?.Cancel();
                oldRetryCts?.Dispose();

                newRetryCts.CancelAfter(_restartTimeout);
                var enumerable = await _enumerableFactory(newRetryCts.Token).ConfigureAwait(false);
                var oldEnumerator = Interlocked.Exchange(ref _innerEnumerator, enumerable.GetAsyncEnumerator(newRetryCts.Token));
                if (oldEnumerator is not null)
                {
                    await oldEnumerator.DisposeAsync().ConfigureAwait(false);
                }
            }

            try
            {
                return await _innerEnumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!_cancellationToken.IsCancellationRequested)
            {
                // If only the retry token cancelled, we want to restart enumerating the sequence
                var newRetryCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                var oldRetryCts = Interlocked.Exchange(ref _restartCts, newRetryCts);
                oldRetryCts?.Cancel();
                oldRetryCts?.Dispose();

                newRetryCts.CancelAfter(_restartTimeout);

                var enumerable = await _enumerableFactory(newRetryCts.Token).ConfigureAwait(false);

                await Interlocked.Exchange(ref _innerEnumerator, enumerable.GetAsyncEnumerator(newRetryCts.Token)).DisposeAsync().ConfigureAwait(false);

                return await _innerEnumerator.MoveNextAsync().ConfigureAwait(false);
            }
        }
    }
}
