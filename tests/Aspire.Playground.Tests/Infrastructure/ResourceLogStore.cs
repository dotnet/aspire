// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;

namespace SamplesIntegrationTests.Infrastructure;

public class ResourceLogStore
{
    private readonly ConcurrentDictionary<IResource, List<LogLine>> _logs = [];

    internal void Add(IResource resource, IEnumerable<LogLine> logs)
    {
        _logs.GetOrAdd(resource, _ => []).AddRange(logs);
    }

    /// <summary>
    /// Gets a snapshot of the logs for all resources.
    /// </summary>
    public IReadOnlyDictionary<IResource, IReadOnlyList<LogLine>> GetLogs() =>
        _logs.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<LogLine>)entry.Value);

    /// <summary>
    /// Gets the logs for the specified resource in the application.
    /// </summary>
    public IReadOnlyList<LogLine> GetLogs(string resourceName)
    {
        var resource = _logs.Keys.FirstOrDefault(k => string.Equals(k.Name, resourceName, StringComparison.OrdinalIgnoreCase));
        if (resource is not null && _logs.TryGetValue(resource, out var logs))
        {
            return logs;
        }
        return [];
    }

    /// <summary>
    /// Ensures no errors were logged for the specified resource.
    /// </summary>
    public void EnsureNoErrors(string resourceName)
    {
        EnsureNoErrors(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensures no errors were logged for the specified resources.
    /// </summary>
    public void EnsureNoErrors(Func<IResource, bool>? resourcePredicate = null, bool throwIfNoResourcesMatch = false)
    {
        var logStore = GetLogs();

        var resourcesMatched = 0;
        foreach (var (resource, logs) in logStore)
        {
            if (resourcePredicate is null || resourcePredicate(resource))
            {
                EnsureNoErrors(resource, logs);
                resourcesMatched++;
            }
        }

        if (throwIfNoResourcesMatch && resourcesMatched == 0 && resourcePredicate is not null)
        {
            throw new ArgumentException("No resources matched the predicate.", nameof(resourcePredicate));
        }

        static void EnsureNoErrors(IResource resource, IEnumerable<LogLine> logs)
        {
            var errors = logs.Where(l => l.IsErrorMessage).ToList();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Resource '{resource.Name}' logged errors: {Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => e.Content))}");
            }
        }
    }
}
