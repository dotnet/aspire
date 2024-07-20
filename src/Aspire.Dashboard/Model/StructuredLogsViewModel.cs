// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

public class StructuredLogsViewModel
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly List<LogFilter> _filters = new();

    private PagedResult<OtlpLogEntry>? _logs;
    private ApplicationKey? _applicationKey;
    private string _filterText = string.Empty;
    private int _logsStartIndex;
    private int? _logsCount;
    private LogLevel? _logLevel;

    public StructuredLogsViewModel(TelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public ApplicationKey? ApplicationKey { get => _applicationKey; set => SetValue(ref _applicationKey, value); }
    public string FilterText { get => _filterText; set => SetValue(ref _filterText, value); }
    public IReadOnlyList<LogFilter> Filters => _filters;

    public void ClearFilters()
    {
        _filters.Clear();
        _logs = null;
    }

    public void AddFilters(IEnumerable<LogFilter> filters)
    {
        _filters.AddRange(filters);
        _logs = null;
    }

    public void AddFilter(LogFilter filter)
    {
        _filters.Add(filter);
        _logs = null;
    }
    public bool RemoveFilter(LogFilter filter)
    {
        if (_filters.Remove(filter))
        {
            _logs = null;
            return true;
        }
        return false;
    }
    public int StartIndex { get => _logsStartIndex; set => SetValue(ref _logsStartIndex, value); }
    public int? Count { get => _logsCount; set => SetValue(ref _logsCount, value); }
    public LogLevel? LogLevel { get => _logLevel; set => SetValue(ref _logLevel, value); }

    private void SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        _logs = null;
    }

    public PagedResult<OtlpLogEntry> GetLogs()
    {
        var logs = _logs;
        if (logs == null)
        {
            var filters = Filters.ToList();
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                filters.Add(new LogFilter { Field = nameof(OtlpLogEntry.Message), Condition = FilterCondition.Contains, Value = FilterText });
            }
            // If the log level is set and it is not the bottom level, which has no effect, then add a filter.
            if (_logLevel != null && _logLevel != Microsoft.Extensions.Logging.LogLevel.Trace)
            {
                filters.Add(new LogFilter { Field = nameof(OtlpLogEntry.Severity), Condition = FilterCondition.GreaterThanOrEqual, Value = _logLevel.Value.ToString() });
            }

            logs = _telemetryRepository.GetLogs(new GetLogsContext
            {
                ApplicationKey = ApplicationKey,
                StartIndex = StartIndex,
                Count = Count,
                Filters = filters
            });
        }

        return logs;
    }

    public void ClearData()
    {
        _logs = null;
    }
}
