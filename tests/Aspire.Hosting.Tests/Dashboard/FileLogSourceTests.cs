// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.Dashboard;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class FileLogSourceTests
{
    [Fact]
    public async Task Read_LifetimeCancellation_Complete()
    {
        // Arrange
        var outPath = Path.GetTempFileName();
        var errorPath = Path.GetTempFileName();
        var cts = new CancellationTokenSource();
        var s = new FileLogSource(outPath, errorPath, cts.Token);
        var channel = Channel.CreateUnbounded<IReadOnlyList<(string Content, bool IsErrorMessage)>>();
        var readTask = Task.Run(async () =>
        {
            await foreach (var item in s)
            {
                await channel.Writer.WriteAsync(item);
            }
            channel.Writer.Complete();
        });

        // Act
        cts.Cancel();
        await readTask;
        var all = await ReadResultsAsync(channel.Reader.ReadAllAsync());

        // Assert
        Assert.Empty(all);
    }

    [Fact]
    public async Task Read_EnumerableCancellation_Complete()
    {
        // Arrange
        var outPath = Path.GetTempFileName();
        var errorPath = Path.GetTempFileName();
        var cts = new CancellationTokenSource();
        var s = new FileLogSource(outPath, errorPath, CancellationToken.None);
        var channel = Channel.CreateUnbounded<IReadOnlyList<(string Content, bool IsErrorMessage)>>();
        var readTask = Task.Run(async () =>
        {
            await foreach (var item in s.WithCancellation(cts.Token))
            {
                await channel.Writer.WriteAsync(item);
            }
            channel.Writer.Complete();
        });

        // Act
        cts.Cancel();
        await readTask;
        var all = await ReadResultsAsync(channel.Reader.ReadAllAsync());

        // Assert
        Assert.Empty(all);
    }

    [Fact]
    public async Task Read_NewFileWithLines_ReturnResults()
    {
        // Arrange
        var outPath = Path.GetTempFileName();
        var errorPath = Path.GetTempFileName();
        var cts = new CancellationTokenSource();
        var s = new FileLogSource(outPath, errorPath, CancellationToken.None);
        var channel = Channel.CreateUnbounded<IReadOnlyList<(string Content, bool IsErrorMessage)>>();
        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in s.WithCancellation(cts.Token))
                {
                    await channel.Writer.WriteAsync(item);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        });

        try
        {
            // Act
            using var outStream = File.CreateText(outPath);
            using var errorStream = File.CreateText(errorPath);

            await outStream.WriteLineAsync("Out 1");
            await outStream.WriteLineAsync("Out 2");
            await outStream.FlushAsync();
            var outResults = await ReadResultsAsync(channel.Reader.ReadAllAsync(), readAtLeast: 2);

            await errorStream.WriteLineAsync("Error 1");
            await errorStream.WriteLineAsync("Error 2");
            await errorStream.FlushAsync();
            var errorResults = await ReadResultsAsync(channel.Reader.ReadAllAsync(), readAtLeast: 2);

            cts.Cancel();
            try { await readTask; } catch (OperationCanceledException) { }
            var completeResults = await ReadResultsAsync(channel.Reader.ReadAllAsync());

            // Assert
            Assert.Collection(outResults,
                r =>
                {
                    Assert.Equal("Out 1", r.Content);
                    Assert.False(r.IsErrorMessage);
                },
                r =>
                {
                    Assert.Equal("Out 2", r.Content);
                    Assert.False(r.IsErrorMessage);
                });
            Assert.Collection(errorResults,
                r =>
                {
                    Assert.Equal("Error 1", r.Content);
                    Assert.True(r.IsErrorMessage);
                },
                r =>
                {
                    Assert.Equal("Error 2", r.Content);
                    Assert.True(r.IsErrorMessage);
                });
            Assert.Empty(completeResults);
        }
        finally
        {
            File.Delete(outPath);
            File.Delete(errorPath);
        }
    }

    public static async Task<List<(string Content, bool IsErrorMessage)>> ReadResultsAsync(IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> asyncEnumerable, int? readAtLeast = null)
    {
        ArgumentNullException.ThrowIfNull(asyncEnumerable);

        var list = new List<(string Content, bool IsErrorMessage)>();
        await foreach (var t in asyncEnumerable)
        {
            foreach (var item in t)
            {
                list.Add(item);

                if (readAtLeast != null && list.Count >= readAtLeast)
                {
                    return list;
                }
            }
        }

        return list;
    }
}
