// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Token : IAsyncDisposable
{
    private static readonly string? s_version = typeof(Token).Assembly.GetDisplayVersion();
    private IJSObjectReference? _jsModule;
    private FluentTextField? _tokenTextField;

    private TokenFormModel _formModel = default!;
    public EditContext EditContext { get; private set; } = default!;

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState is { } authStateTask)
        {
            var state = await authStateTask;
            if (state.User.Identity?.IsAuthenticated ?? false)
            {
                NavigationManager.NavigateTo(ReturnUrl ?? "/", forceLoad: true);
                return;
            }
        }

        _formModel = new TokenFormModel();
        EditContext = new EditContext(_formModel);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/Token.razor.js");

            _tokenTextField?.FocusAsync();
        }
    }

    private async Task GetToken()
    {
        if (_jsModule is null)
        {
            return;
        }

        var result = await _jsModule.InvokeAsync<string>("validateToken", _formModel.Token);

        if (bool.TryParse(result, out var success) && success)
        {
            NavigationManager.NavigateTo(ReturnUrl ?? "/", forceLoad: true);
            return;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await JSInteropHelpers.SafeDisposeAsync(_jsModule);
    }
}
