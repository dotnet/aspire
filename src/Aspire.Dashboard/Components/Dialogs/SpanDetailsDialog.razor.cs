// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;
public partial class SpanDetailsDialog
{
    [Parameter]
    public SpanDetailsDialogViewModel Content { get; set; } = default!;

    [CascadingParameter]
    public FluentDialog? Dialog { get; set; }

    private IQueryable<SpanPropertyViewModel>? FilteredItems =>
        Content.Properties.Where(vm =>
            vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        )?.AsQueryable();

    private IQueryable<SpanPropertyViewModel>? FilteredApplicationItems =>
        Content.Span.Source.AllProperties().Select(p => new SpanPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(vm =>
                vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
                vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        )?.AsQueryable();

    private string _filter = "";

    private readonly GridSort<SpanPropertyViewModel> _nameSort = GridSort<SpanPropertyViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<SpanPropertyViewModel> _valueSort = GridSort<SpanPropertyViewModel>.ByAscending(vm => vm.Value);

    private void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            _filter = newFilter;
        }
    }

    private void HandleClear()
    {
        _filter = string.Empty;
    }
}
