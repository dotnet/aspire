// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;
public partial class LogDetailsDialog
{
    [Parameter]
    public List<LogEntryPropertyViewModel> Content { get; set; } = default!;

    private IQueryable<LogEntryPropertyViewModel>? FilteredItems =>
        Content?.Where(vm =>
            vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        )?.AsQueryable();

    private string _filter = "";

    private readonly GridSort<LogEntryPropertyViewModel> _nameSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<LogEntryPropertyViewModel> _valueSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Value);

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
