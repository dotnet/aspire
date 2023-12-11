// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class TraceDetail : ComponentBase
{
    private readonly List<IDisposable> _peerChangesSubscriptions = new();
    private OtlpTrace? _trace;
    private OtlpSpan? _span;
    private Subscription? _tracesSubscription;
    private List<SpanWaterfallViewModel>? _spanWaterfallViewModels;
    private int _maxDepth;
    private List<OtlpApplication> _applications = default!;

    [Parameter]
    public required string TraceId { get; set; }

    [Parameter]
    public string? SpanId { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required IEnumerable<IOutgoingPeerResolver> OutgoingPeerResolvers { get; set; }

    protected override void OnInitialized()
    {
        foreach (var resolver in OutgoingPeerResolvers)
        {
            _peerChangesSubscriptions.Add(resolver.OnPeerChanges(async () =>
            {
                UpdateDetailViewData();
                await InvokeAsync(StateHasChanged);
            }));
        }
    }

    private ValueTask<GridItemsProviderResult<SpanWaterfallViewModel>> GetData(GridItemsProviderRequest<SpanWaterfallViewModel> request)
    {
        Debug.Assert(_spanWaterfallViewModels != null);

        return ValueTask.FromResult(new GridItemsProviderResult<SpanWaterfallViewModel>
        {
            Items = _spanWaterfallViewModels,
            TotalItemCount = _spanWaterfallViewModels.Count
        });
    }

    private static List<SpanWaterfallViewModel> CreateSpanWaterfallViewModels(OtlpTrace trace, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        var orderedSpans = new List<SpanWaterfallViewModel>();
        // There should be one root span but just in case, we'll add them all.
        foreach (var rootSpan in trace.Spans.Where(s => string.IsNullOrEmpty(s.ParentSpanId)).OrderBy(s => s.StartTime))
        {
            AddSelfAndChildren(orderedSpans, rootSpan, depth: 1, outgoingPeerResolvers, CreateViewModel);
        }
        // Unparented spans.
        foreach (var unparentedSpan in trace.Spans.Where(s => !string.IsNullOrEmpty(s.ParentSpanId) && s.GetParentSpan() == null).OrderBy(s => s.StartTime))
        {
            AddSelfAndChildren(orderedSpans, unparentedSpan, depth: 1, outgoingPeerResolvers, CreateViewModel);
        }

        return orderedSpans;

        static void AddSelfAndChildren(List<SpanWaterfallViewModel> orderedSpans, OtlpSpan span, int depth, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers, Func<OtlpSpan, int, IEnumerable<IOutgoingPeerResolver>, SpanWaterfallViewModel> createViewModel)
        {
            orderedSpans.Add(createViewModel(span, depth, outgoingPeerResolvers));
            depth++;

            foreach (var child in span.GetChildSpans().OrderBy(s => s.StartTime))
            {
                AddSelfAndChildren(orderedSpans, child, depth, outgoingPeerResolvers, createViewModel);
            }
        }

        static SpanWaterfallViewModel CreateViewModel(OtlpSpan span, int depth, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
        {
            var traceStart = span.Trace.FirstSpan.StartTime;
            var relativeStart = span.StartTime - traceStart;
            var rootDuration = span.Trace.Duration.TotalMilliseconds;

            var leftOffset = relativeStart.TotalMilliseconds / rootDuration * 100;
            var width = span.Duration.TotalMilliseconds / rootDuration * 100;

            // Figure out if the label is displayed to the left or right of the span.
            // If the label position is based on whether more than half of the span is on the left or right side of the trace.
            var labelIsRight = (relativeStart + span.Duration / 2) < (span.Trace.Duration / 2);

            // A span may indicate a call to another service but the service isn't instrumented.
            var hasPeerService = span.Attributes.Any(a => a.Key == OtlpSpan.PeerServiceAttributeKey);
            var isUninstrumentedPeer = hasPeerService && span.Kind is OtlpSpanKind.Client or OtlpSpanKind.Producer && !span.GetChildSpans().Any();
            var uninstrumentedPeer = isUninstrumentedPeer ? ResolveUninstrumentedPeerName(span, outgoingPeerResolvers) : null;

            var viewModel = new SpanWaterfallViewModel
            {
                Span = span,
                LeftOffset = leftOffset,
                Width = width,
                Depth = depth,
                LabelIsRight = labelIsRight,
                UninstrumentedPeer = uninstrumentedPeer
            };
            return viewModel;
        }
    }

    private static string? ResolveUninstrumentedPeerName(OtlpSpan span, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        // Attempt to resolve uninstrumented peer to a friendly name from the span.
        foreach (var resolver in outgoingPeerResolvers)
        {
            if (resolver.TryResolvePeerName(span.Attributes, out var name))
            {
                return name;
            }
        }

        // Fallback to the peer address.
        return OtlpHelpers.GetValue(span.Attributes, OtlpSpan.PeerServiceAttributeKey);
    }

    protected override void OnParametersSet()
    {
        UpdateDetailViewData();
    }

    private void UpdateDetailViewData()
    {
        _applications = TelemetryRepository.GetApplications();

        _trace = null;
        _span = null;

        if (TraceId is not null)
        {
            _trace = TelemetryRepository.GetTrace(TraceId);
            if (_trace != null)
            {
                _spanWaterfallViewModels = CreateSpanWaterfallViewModels(_trace, OutgoingPeerResolvers);
                _maxDepth = _spanWaterfallViewModels.Max(s => s.Depth);

                if (_tracesSubscription is null || _tracesSubscription.ApplicationId != _trace.FirstSpan.Source.InstanceId)
                {
                    _tracesSubscription?.Dispose();
                    _tracesSubscription = TelemetryRepository.OnNewTraces(_trace.FirstSpan.Source.InstanceId, SubscriptionType.Read, () => InvokeAsync(() =>
                    {
                        UpdateDetailViewData();
                        StateHasChanged();
                        return Task.CompletedTask;
                    }));
                }
                if (SpanId is not null)
                {
                    _span = _trace.Spans.FirstOrDefault(s => s.SpanId.StartsWith(SpanId, StringComparison.Ordinal));
                }
            }
        }
    }

    private string GetRowClass(SpanWaterfallViewModel viewModel)
    {
        if (viewModel.Span == SelectedSpan?.Span)
        {
            return "selected-row";
        }
        else
        {
            return (viewModel.Span.SpanId == _span?.SpanId) ? "selected-span" : string.Empty;
        }
    }

    public SpanDetailsViewModel? SelectedSpan { get; set; }

    private void OnShowProperties(SpanWaterfallViewModel viewModel)
    {
        if (SelectedSpan?.Span == viewModel.Span)
        {
            ClearSelectedSpan();
        }
        else
        {
            var entryProperties = viewModel.Span.AllProperties()
                .Select(kvp => new SpanPropertyViewModel { Name = kvp.Key, Value = kvp.Value })
                .ToList();

            var spanDetailsViewModel = new SpanDetailsViewModel
            {
                Span = viewModel.Span,
                Properties = entryProperties,
                Title = $"{GetResourceName(viewModel.Span.Source)}: {viewModel.GetDisplaySummary()}"
            };

            SelectedSpan = spanDetailsViewModel;
        }
    }

    private void ClearSelectedSpan()
    {
        SelectedSpan = null;
    }

    private string GetResourceName(OtlpApplication app) => OtlpApplication.GetResourceName(app, _applications);

    public void Dispose()
    {
        foreach (var subscription in _peerChangesSubscriptions)
        {
            subscription.Dispose();
        }
        _tracesSubscription?.Dispose();
    }
}
