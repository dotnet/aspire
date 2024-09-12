// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

internal static class ConsoleLoggingTestHelpers
{
    public static Task<IReadOnlyList<LogLine>> WatchForLogsAsync(ResourceLoggerService service, int targetLogCount, IResource resource)
    {
        var watchEnumerable = service.WatchAsync(resource);
        return WatchForLogsAsync(watchEnumerable, targetLogCount);
    }

    public static Task<IReadOnlyList<LogLine>> WatchForLogsAsync(IAsyncEnumerable<IReadOnlyList<LogLine>> watchEnumerable, int targetLogCount)
    {
        return Task.Run(async () =>
        {
            var logs = new List<LogLine>();
            await foreach (var log in watchEnumerable)
            {
                logs.AddRange(log);
                if (logs.Count >= targetLogCount)
                {
                    break;
                }
            }
            return (IReadOnlyList<LogLine>)logs;
        });
    }

    public static Task<IReadOnlyList<LogLine>> WatchForLogsAsync(IAsyncEnumerator<IReadOnlyList<LogLine>> watchEnumerator, int targetLogCount)
    {
        return Task.Run(async () =>
        {
            var logs = new List<LogLine>();
            while (await watchEnumerator.MoveNextAsync())
            {
                logs.AddRange(watchEnumerator.Current);
                if (logs.Count >= targetLogCount)
                {
                    break;
                }
            }

            return (IReadOnlyList<LogLine>)logs;
        });
    }

    public static ResourceLoggerService GetResourceLoggerService()
    {
        return new ResourceLoggerService
        {
            TimeProvider = new TestTimeProvider()
        };
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        public static TestTimeProvider Instance = new TestTimeProvider();

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2000, 12, 29, 20, 59, 59, TimeSpan.Zero);
        }
    }
}
