// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

public class TracesViewModel
{
    private readonly TelemetryRepository _telemetryRepository;

    private PagedResult<OtlpTrace>? _traces;
    private string? _applicationServiceId;
    private string _filterText = string.Empty;
    private int _startIndex;
    private int? _count;

    public TracesViewModel(TelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public string? ApplicationServiceId { get => _applicationServiceId; set => SetValue(ref _applicationServiceId, value); }
    public string FilterText { get => _filterText; set => SetValue(ref _filterText, value); }
    public int StartIndex { get => _startIndex; set => SetValue(ref _startIndex, value); }
    public int? Count { get => _count; set => SetValue(ref _count, value); }
    public TimeSpan MaxDuration { get; private set; }

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
            var result = _telemetryRepository.GetTraces(new GetTracesRequest
            {
                ApplicationServiceId = ApplicationServiceId,
                FilterText = FilterText,
                StartIndex = StartIndex,
                Count = Count
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

