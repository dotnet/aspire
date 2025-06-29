// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Shared;

public class TestDialogService : IDialogService
{
    private readonly Func<object, DialogParameters, Task<IDialogReference>>? _onShowDialog;

    public TestDialogService(Func<object, DialogParameters, Task<IDialogReference>>? onShowDialog = null)
    {
        _onShowDialog = onShowDialog;
    }

#pragma warning disable CS0067
    public event Action<IDialogReference, Type?, DialogParameters, object>? OnShow;
    public event Func<IDialogReference, Type?, DialogParameters, object, Task<IDialogReference>>? OnShowAsync;
    public event Action<string, DialogParameters>? OnUpdate;
    public event Func<string, DialogParameters, Task<IDialogReference?>>? OnUpdateAsync;
    public event Action<IDialogReference, DialogResult>? OnDialogCloseRequested;
#pragma warning restore CS0067

    public Task CloseAsync(DialogReference dialog)
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync(DialogReference dialog, DialogResult result)
    {
        throw new NotImplementedException();
    }

    public EventCallback<DialogResult> CreateDialogCallback(object receiver, Func<DialogResult, Task> callback)
    {
        throw new NotImplementedException();
    }

    public void ShowConfirmation(object receiver, Func<DialogResult, Task> callback, string message, string primaryText = "Yes", string secondaryText = "No", string? title = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowConfirmationAsync(object receiver, Func<DialogResult, Task> callback, string message, string primaryText = "Yes", string secondaryText = "No", string? title = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowConfirmationAsync(string message, string primaryText = "Yes", string secondaryText = "No", string? title = null)
    {
        throw new NotImplementedException();
    }

    public void ShowDialog<TDialog, TData>(DialogParameters<TData> parameters)
        where TDialog : IDialogContentComponent<TData>
        where TData : class
    {
        throw new NotImplementedException();
    }

    public void ShowDialog<TData>(Type dialogComponent, TData data, DialogParameters parameters) where TData : class
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowDialogAsync<TData>(Type dialogComponent, TData data, DialogParameters parameters) where TData : class
    {
        return _onShowDialog?.Invoke(data, parameters) ?? throw new InvalidOperationException("No dialog callback specified.");
    }

    public Task<IDialogReference> ShowDialogAsync<TDialog>(object data, DialogParameters parameters) where TDialog : IDialogContentComponent
    {
        return _onShowDialog?.Invoke(data, parameters) ?? throw new InvalidOperationException("No dialog callback specified.");
    }

    public Task<IDialogReference> ShowDialogAsync<TDialog>(DialogParameters parameters) where TDialog : IDialogContentComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowDialogAsync(RenderFragment renderFragment, DialogParameters dialogParameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowDialogAsync<TDialog, TData>(DialogParameters<TData> parameters)
        where TDialog : IDialogContentComponent<TData>
        where TData : class
    {
        throw new NotImplementedException();
    }

    public void ShowError(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowErrorAsync(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public void ShowInfo(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowInfoAsync(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public void ShowMessageBox(DialogParameters<MessageBoxContent> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowMessageBoxAsync(DialogParameters<MessageBoxContent> parameters)
    {
        throw new NotImplementedException();
    }

    public void ShowPanel<TDialog, TData>(DialogParameters<TData> parameters)
        where TDialog : IDialogContentComponent<TData>
        where TData : class
    {
        throw new NotImplementedException();
    }

    public void ShowPanel<TData>(Type dialogComponent, DialogParameters<TData> parameters) where TData : class
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowPanelAsync<TData>(Type dialogComponent, TData data, DialogParameters parameters) where TData : class
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowPanelAsync<TDialog>(object data, DialogParameters parameters) where TDialog : IDialogContentComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowPanelAsync<TDialog>(DialogParameters parameters) where TDialog : IDialogContentComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowPanelAsync<TDialog, TData>(DialogParameters<TData> parameters)
        where TDialog : IDialogContentComponent<TData>
        where TData : class
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowPanelAsync<TData>(Type dialogComponent, DialogParameters<TData> parameters) where TData : class
    {
        throw new NotImplementedException();
    }

    public void ShowSplashScreen(object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public void ShowSplashScreen<T>(object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters) where T : IDialogContentComponent<SplashScreenContent>
    {
        throw new NotImplementedException();
    }

    public void ShowSplashScreen(Type component, object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync(object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync(DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync<T>(object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters) where T : IDialogContentComponent<SplashScreenContent>
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync<T>(DialogParameters<SplashScreenContent> parameters) where T : IDialogContentComponent<SplashScreenContent>
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync(Type component, object receiver, Func<DialogResult, Task> callback, DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSplashScreenAsync(Type component, DialogParameters<SplashScreenContent> parameters)
    {
        throw new NotImplementedException();
    }

    public void ShowSuccess(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowSuccessAsync(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public void ShowWarning(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowWarningAsync(string message, string? title = null, string? primaryText = null)
    {
        throw new NotImplementedException();
    }

    public void UpdateDialog<TData>(string id, DialogParameters<TData> parameters) where TData : class
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference?> UpdateDialogAsync<TData>(string id, DialogParameters<TData> parameters) where TData : class
    {
        throw new NotImplementedException();
    }
}
