// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Login : IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private FluentTextField? _tokenTextField;
    private ValidationMessageStore? _messageStore;

    private TokenFormModel _formModel = default!;
    public EditContext EditContext { get; private set; } = default!;

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required ILogger<Login> Logger { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // If the browser is already authenticated then redirect to the app.
        if (AuthenticationState is { } authStateTask)
        {
            var state = await authStateTask;
            if (state.User.Identity?.IsAuthenticated ?? false)
            {
                NavigationManager.NavigateTo(GetRedirectUrl(), forceLoad: true);
                return;
            }
        }

        _formModel = new TokenFormModel();
        EditContext = new EditContext(_formModel);
        _messageStore = new(EditContext);
        EditContext.OnValidationRequested += (s, e) => _messageStore.Clear();
        EditContext.OnFieldChanged += (s, e) => _messageStore.Clear(e.FieldIdentifier);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/Login.razor.js");

            _tokenTextField?.FocusAsync();
        }
    }

    private async Task SubmitAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        // Invoke a JS function to validate the token. This is required because a cookie can't be set from a SignalR connection.
        // The JS function calls an API back on the server to validate the token and that API call sets the cookie.
        // Because the browser made the API call the cookie is set in the browser.
        var result = await _jsModule.InvokeAsync<string>("validateToken", _formModel.Token);

        if (bool.TryParse(result, out var success))
        {
            if (success)
            {
                NavigationManager.NavigateTo(GetRedirectUrl(), forceLoad: true);
                return;
            }
            else
            {
                _messageStore?.Add(() => _formModel.Token!, Loc[nameof(Dashboard.Resources.Login.InvalidTokenErrorMessage)]);
            }
        }
        else
        {
            Logger.LogWarning("Unexpected result from validateToken: {Result}", result);
            _messageStore?.Add(() => _formModel.Token!, Loc[nameof(Dashboard.Resources.Login.UnexpectedValidationError)]);
        }
    }

    private string GetRedirectUrl()
    {
        return ReturnUrl ?? DashboardUrls.ResourcesUrl();
    }

    public async ValueTask DisposeAsync()
    {
        await JSInteropHelpers.SafeDisposeAsync(_jsModule);
    }
}
