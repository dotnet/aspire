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
    private string? _applicationServiceId;
    private string _filterText = string.Empty;
    private int _logsStartIndex;
    private int? _logsCount;

    public StructuredLogsViewModel(TelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public string? ApplicationServiceId { get => _applicationServiceId; set => SetValue(ref _applicationServiceId, value); }
    public string FilterText { get => _filterText; set => SetValue(ref _filterText, value); }
    public IReadOnlyList<LogFilter> Filters => _filters;
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
                filters.Add(new LogFilter { Field = "Message", Condition = FilterCondition.Contains, Value = FilterText });
            }

            logs = _telemetryRepository.GetLogs(new GetLogsContext
            {
                ApplicationServiceId = ApplicationServiceId,
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

