// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;
public partial class SpanDetailsDialog
{
    [Parameter]
    public SpanDetailsDialogViewModel Content { get; set; } = default!;

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

    private const string PreCopyText = "Copy to clipboard";
    private const string PostCopyText = "Copied!";

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

    private void HandleClear(string? value)
    {
        _filter = value ?? string.Empty;
    }

    private async Task CopyTextToClipboardAsync(string? text, string id)
    {
        await JS.InvokeVoidAsync("copyTextToClipboard", id, text, PreCopyText, PostCopyText);
    }
}