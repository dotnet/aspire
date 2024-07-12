// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class SpanDetails
{
    [Parameter, EditorRequired]
    public required SpanDetailsViewModel ViewModel { get; set; }

    private IQueryable<SpanPropertyViewModel> FilteredItems =>
        ViewModel.Properties.Where(ApplyFilter).AsQueryable();

    private IQueryable<SpanPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Select(p => new SpanPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<SpanPropertyViewModel> FilteredResourceItems =>
        ViewModel.Span.Source.AllProperties().Select(p => new SpanPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<OtlpSpanEvent> FilteredSpanEvents =>
        ViewModel.Span.Events.Where(e => e.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(e => e.Time).AsQueryable();

    private string _filter = "";
    private List<KeyValuePair<string, string>> _contextAttributes = null!;

    private readonly GridSort<SpanPropertyViewModel> _nameSort = GridSort<SpanPropertyViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<SpanPropertyViewModel> _valueSort = GridSort<SpanPropertyViewModel>.ByAscending(vm => vm.Value);

    private bool ApplyFilter(SpanPropertyViewModel vm)
    {
        return vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    protected override void OnParametersSet()
    {
        _contextAttributes =
        [
            new KeyValuePair<string, string>("Source", ViewModel.Span.Scope.ScopeName)
        ];
        if (!string.IsNullOrEmpty(ViewModel.Span.Scope.Version))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("Version", ViewModel.Span.Scope.Version));
        }
        if (!string.IsNullOrEmpty(ViewModel.Span.ParentSpanId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("ParentId", ViewModel.Span.ParentSpanId));
        }
        if (!string.IsNullOrEmpty(ViewModel.Span.TraceId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("TraceId", ViewModel.Span.TraceId));
        }
    }
}
