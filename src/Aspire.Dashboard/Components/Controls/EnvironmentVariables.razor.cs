// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class EnvironmentVariables
{

    [Parameter, EditorRequired]
    public IEnumerable<EnvironmentVariableViewModel>? Items { get; set; }

    [Parameter]
    public bool ShowSpecOnlyToggle { get; set; }

    private bool _showAll;

    private IQueryable<EnvironmentVariableViewModel>? FilteredItems =>
        Items?.Where(vm =>
            (_showAll || vm.FromSpec) &&
            (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        )?.AsQueryable();

    private string _filter = "";
    private bool _defaultMasked = true;

    private readonly Icon _maskIcon = new Icons.Regular.Size16.EyeOff();
    private readonly Icon _unmaskIcon = new Icons.Regular.Size16.Eye();
    private readonly Icon _showSpecOnlyIcon = new Icons.Regular.Size16.DocumentHeader();
    private readonly Icon _showAllIcon = new Icons.Regular.Size16.DocumentOnePage();

    private readonly GridSort<EnvironmentVariableViewModel> _nameSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<EnvironmentVariableViewModel> _valueSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Value);

    private void ToggleMaskState()
    {
        _defaultMasked = !_defaultMasked;
        if (Items is not null)
        {
            foreach (var vm in Items)
            {
                vm.IsValueMasked = _defaultMasked;
            }
        }
    }

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

    private void CheckAllMaskStates()
    {
        if (Items is not null)
        {
            var foundMasked = false;
            var foundUnmasked = false;
            foreach (var vm in Items)
            {
                foundMasked |= vm.IsValueMasked;
                foundUnmasked |= !vm.IsValueMasked;
            }

            if (!foundMasked && foundUnmasked)
            {
                _defaultMasked = false;
            }
            else if (foundMasked && !foundUnmasked)
            {
                _defaultMasked = true;
            }
        }
    }
}
