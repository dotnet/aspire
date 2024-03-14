// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class SummaryDetailsView<T> : IGlobalKeydownListener, IDisposable
{
    [Parameter]
    public RenderFragment? Summary { get; set; }

    [Parameter]
    public RenderFragment? Details { get; set; }

    [Parameter]
    public bool ShowDetails { get; set; }

    [Parameter]
    public string? DetailsTitle { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    [Parameter]
    public bool RememberSize { get; set; } = true;

    [Parameter]
    public bool RememberOrientation { get; set; } = true;

    [Parameter, EditorRequired]
    public T? SelectedValue { get; set; }

    /// <summary>
    /// Overrides the default key used to store the splitter size and orientation in local storage.
    /// By default, the key is based on the current URL. If you have multiple instances of this control
    /// on a page or want to share the same settings across multiple pages, you can set this property
    /// </summary>
    [Parameter]
    public string? ViewKey { get; set; }

    [Parameter]
    public RenderFragment? DetailsTitleTemplate { get; set; }

    [Inject]
    public required ProtectedLocalStorage ProtectedLocalStore { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required ShortcutManager ShortcutManager { get; set; }

    private readonly Icon _splitHorizontalIcon = new Icons.Regular.Size16.SplitHorizontal();
    private readonly Icon _splitVerticalIcon = new Icons.Regular.Size16.SplitVertical();

    private string _panel1Size { get; set; } = "1fr";
    private string _panel2Size { get; set; } = "1fr";
    private bool _internalShowDetails;
    private FluentSplitter? _splitterRef;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            ShortcutManager.AddGlobalKeydownListener(this);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // If show details is changing from false to true, read saved state.
        if (ShowDetails && !_internalShowDetails)
        {
            if (RememberOrientation)
            {
                var orientationResult = await ProtectedLocalStore.SafeGetAsync<Orientation>(GetOrientationStorageKey());
                if (orientationResult.Success)
                {
                    Orientation = orientationResult.Value;
                }
            }

            if (RememberSize)
            {
                var panel1FractionResult = await ProtectedLocalStore.SafeGetAsync<float>(GetSizeStorageKey());
                if (panel1FractionResult.Success)
                {
                    SetPanelSizes(panel1FractionResult.Value);
                }
            }
        }

        // Bind visibility to internal bool that is set after reading from local store.
        // This is required because we only want to show details after resolving size and orientation
        // to avoid a flash of content in the wrong location.
        _internalShowDetails = ShowDetails;
    }

    private async Task HandleDismissAsync()
    {
        await OnDismiss.InvokeAsync();
    }

    private async Task HandleToggleOrientation()
    {
        if (Orientation == Orientation.Horizontal)
        {
            Orientation = Orientation.Vertical;
        }
        else
        {
            Orientation = Orientation.Horizontal;
        }

        if (RememberOrientation)
        {
            await ProtectedLocalStore.SetAsync(GetOrientationStorageKey(), Orientation);
        }

        if (RememberSize)
        {
            var panel1FractionResult = await ProtectedLocalStore.SafeGetAsync<float>(GetSizeStorageKey());
            if (panel1FractionResult.Success)
            {
                SetPanelSizes(panel1FractionResult.Value);

            }
            else
            {
                ResetPanelSizes();
            }
        }
        else
        {
            ResetPanelSizes();
        }

        // The FluentSplitter control will render during the async calls above, but with the wrong values.
        // We need to force a re-render to get the correct values.
        StateHasChanged();
    }

    private async Task HandleSplitterResize(SplitterResizedEventArgs args)
    {
        var totalSize = (float)(args.Panel1Size + args.Panel2Size);

        var panel1Fraction = (args.Panel1Size / totalSize);

        SetPanelSizes(panel1Fraction);

        if (RememberSize)
        {
            await SaveSizeToStorage(panel1Fraction);
        }
    }

    private async Task SaveSizeToStorage(float panel1Fraction)
    {
        await ProtectedLocalStore.SetAsync(GetSizeStorageKey(), panel1Fraction);
    }

    private void ResetPanelSizes()
    {
        _panel1Size = "1fr";
        _panel2Size = "1fr";
    }

    private void SetPanelSizes(float panel1Fraction)
    {
        // These need to not use culture-specific formatting because it needs to be a valid CSS value
        _panel1Size = string.Create(CultureInfo.InvariantCulture, $"{panel1Fraction:F3}fr");
        _panel2Size = string.Create(CultureInfo.InvariantCulture, $"{(1 - panel1Fraction):F3}fr");
    }

    public IReadOnlySet<AspireKeyboardShortcut> SubscribedShortcuts { get; } = new HashSet<AspireKeyboardShortcut>
    {
        AspireKeyboardShortcut.ToggleOrientation,
        AspireKeyboardShortcut.ClosePanel,
        AspireKeyboardShortcut.ResetPanelSize,
        AspireKeyboardShortcut.IncreasePanelSize,
        AspireKeyboardShortcut.DecreasePanelSize
    };

    public async Task OnPageKeyDownAsync(AspireKeyboardShortcut shortcut)
    {
        if (_splitterRef is null)
        {
            return;
        }

        if (shortcut is AspireKeyboardShortcut.ToggleOrientation)
        {
            await HandleToggleOrientation();
            return;
        }

        if (shortcut is AspireKeyboardShortcut.ClosePanel)
        {
            if (SelectedValue is not null)
            {
                await HandleDismissAsync();
                await InvokeAsync(StateHasChanged);
            }

            return;
        }

        var hasChanged = false;

        if (shortcut is AspireKeyboardShortcut.ResetPanelSize)
        {
            ResetPanelSizes();
            hasChanged = true;
        }

        GetPanelSizes(_splitterRef.Panel1Size, _splitterRef.Panel2Size, out var panel1Size, out var panel2Size, out var panel1Fraction);

        if (panel1Size is null || panel2Size is null || panel1Fraction is null)
        {
            return;
        }

        if (shortcut is AspireKeyboardShortcut.IncreasePanelSize)
        {
            SetPanelSizes(panel1Fraction.Value - 0.05f);
            hasChanged = true;
        }
        else if (shortcut is AspireKeyboardShortcut.DecreasePanelSize)
        {
            SetPanelSizes(panel1Fraction.Value + 0.05f);
            hasChanged = true;
        }

        GetPanelSizes(_splitterRef.Panel1Size, _splitterRef.Panel2Size, out _, out _, out var newPanel1Fraction);

        if (newPanel1Fraction is null || !hasChanged)
        {
            return;
        }

        await SaveSizeToStorage(newPanel1Fraction.Value);
        await InvokeAsync(StateHasChanged);

        return;

        static void GetPanelSizes(
            string? panel1SizeString,
            string? panel2SizeString,
            out float? panel1Size,
            out float? panel2Size,
            out float? panel1Fraction)
        {
            if (panel1SizeString is null || !panel1SizeString.EndsWith("fr")
                || panel2SizeString is null || !panel2SizeString.EndsWith("fr"))
            {
                panel1Size = null;
                panel2Size = null;
                panel1Fraction = null;
                return;
            }

            panel1Size = (float)Convert.ToDouble(panel1SizeString[..^2], CultureInfo.InvariantCulture);
            panel2Size = (float)Convert.ToDouble(panel2SizeString[..^2], CultureInfo.InvariantCulture);

            var newTotalSize = (float)(panel1Size + panel2Size);
            panel1Fraction = panel1Size.Value / newTotalSize;
        }
    }

    private string GetSizeStorageKey()
    {
        var viewKey = ViewKey ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        return $"SplitterSize_{Orientation}_{viewKey}";
    }

    private string GetOrientationStorageKey()
    {
        var viewKey = ViewKey ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        return $"SplitterOrientation_{viewKey}";
    }

    public void Dispose()
    {
        ShortcutManager.RemoveGlobalKeydownListener(this);
    }
}
