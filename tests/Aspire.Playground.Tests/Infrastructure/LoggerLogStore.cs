using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SamplesIntegrationTests.Infrastructure;

/// <summary>
/// Stores logs from <see cref="ILogger"/> instances created from <see cref="StoredLogsLoggerProvider"/>.
/// </summary>
public class LoggerLogStore(IHostEnvironment hostEnvironment)
{
    private readonly ConcurrentDictionary<string, List<(DateTimeOffset TimeStamp, string Category, LogLevel Level, string Message, Exception? Exception)>> _store = [];

    public void AddLog(string category, LogLevel level, string message, Exception? exception)
    {
        _store.GetOrAdd(category, _ => []).Add((DateTimeOffset.Now, category, level, message, exception));
    }

    public IReadOnlyDictionary<string, IList<(DateTimeOffset TimeStamp, string Category, LogLevel Level, string Message, Exception? Exception)>> GetLogs()
    {
        return _store.ToDictionary(entry => entry.Key, entry => (IList<(DateTimeOffset, string, LogLevel, string, Exception?)>)entry.Value);
    }

    public void EnsureNoErrors()
    {
        var logs = GetLogs();

        var errors = logs.SelectMany(kvp => kvp.Value).Where(log => log.Level == LogLevel.Error || log.Level == LogLevel.Critical).ToList();
        //Where(category => category.Value.Any(log => log.Level == LogLevel.Error || log.Level == LogLevel.Critical)).ToList();
        if (errors.Count > 0)
        {
            var appName = hostEnvironment.ApplicationName;
            throw new InvalidOperationException(
                $"AppHost '{appName}' logged errors: {Environment.NewLine}" +
                string.Join(Environment.NewLine, errors.Select(log => $"[{log.Category}] {log.Message}")));
        }
    }
}
