// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Aspire.Hosting.Extensions;

internal static class ChannelExtensions
{
    /// <summary>
    /// Reads batches, grabbing all available messages and returning them as one batch before yielding.
    /// This can give a better downstream experience, as there's less per-item overhead.
    /// </summary>
    /// <remarks>
    /// This sequence adopts the lifetime of <paramref name="channel"/>.
    /// Callers are required to either use a channel that will complete, or to pass a cancellation
    /// token which will also cancel the sequence returned by this method.
    /// </remarks>
    /// <typeparam name="T">The type of items in the channel and returned batch.</typeparam>
    /// <param name="channel">The channel to read values from.</param>
    /// <param name="cancellationToken">A token that signals a loss of interest in the operation.</param>
    /// <returns></returns>
    public static async IAsyncEnumerable<IReadOnlyList<T>> GetBatches<T>(
        this Channel<T> channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            List<T>? batch = null;

            // Wait until there's something to read, or the channel closes.
            if (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Read everything in the channel into a batch.
                while (!cancellationToken.IsCancellationRequested && channel.Reader.TryRead(out var log))
                {
                    batch ??= [];
                    batch.Add(log);
                }

                if (!cancellationToken.IsCancellationRequested && batch is not null)
                {
                    yield return batch;
                }
            }
            else
            {
                // The channel completed, so there'll be no further data.
                break;
            }
        }
    }
}
