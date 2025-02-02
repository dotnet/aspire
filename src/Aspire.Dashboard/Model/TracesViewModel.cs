// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

public class TracesViewModel
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly List<TelemetryFilter> _filters = new();

    private PagedResult<OtlpTrace>? _traces;
    private ApplicationKey? _applicationKey;
    private string _filterText = string.Empty;
    private int _startIndex;
    private int _count;

    public TracesViewModel(TelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public ApplicationKey? ApplicationKey { get => _applicationKey; set => SetValue(ref _applicationKey, value); }
    public string FilterText { get => _filterText; set => SetValue(ref _filterText, value); }
    public int StartIndex { get => _startIndex; set => SetValue(ref _startIndex, value); }
    public int Count { get => _count; set => SetValue(ref _count, value); }
    public TimeSpan MaxDuration { get; private set; }
    public IReadOnlyList<TelemetryFilter> Filters => _filters;

    public void ClearFilters()
    {
        _filters.Clear();
        _traces = null;
    }

    public void AddFilter(TelemetryFilter filter)
    {
        // Don't add duplicate filters.
        foreach (var existingFilter in _filters)
        {
            if (existingFilter.Equals(filter))
            {
                return;
            }
        }

        _filters.Add(filter);
        _traces = null;
    }

    public bool RemoveFilter(TelemetryFilter filter)
    {
        if (_filters.Remove(filter))
        {
            _traces = null;
            return true;
        }
        return false;
    }

    private void SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        _traces = null;
    }

    public PagedResult<OtlpTrace> GetTraces()
    {
        var traces = _traces;
        if (traces == null)
        {
            var filters = Filters.ToList();

            var result = _telemetryRepository.GetTraces(new GetTracesRequest
            {
                ApplicationKey = ApplicationKey,
                FilterText = FilterText,
                StartIndex = StartIndex,
                Count = Count,
                Filters = filters
            });

            traces = result.PagedResult;
            MaxDuration = result.MaxDuration;
        }

        return traces;
    }

    public void ClearData()
    {
        _traces = null;
    }
}

