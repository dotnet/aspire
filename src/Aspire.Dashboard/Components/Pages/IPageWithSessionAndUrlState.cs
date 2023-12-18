// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.WebUtilities;

namespace Aspire.Dashboard.Components.Pages;

/// <summary>
/// Represents a page that can contain state both in the url and in localstorage.
/// Navigating back to the page will restore the previous page state
/// </summary>
/// <typeparam name="TViewModel">The view model containing live state</typeparam>
/// <typeparam name="TSerializableViewModel">A serializable version of <see cref="TViewModel"/> that will be saved in session storage and restored from</typeparam>
public interface IPageWithSessionAndUrlState<TViewModel, TSerializableViewModel>
    where TSerializableViewModel : class
{
    /// <summary>
    /// The base relative path of the page (ie, Metrics for /Metrics)
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    /// The key to save page state to
    /// </summary>
    public string SessionStorageKey { get; }

    public NavigationManager NavigationManager { get; set; }
    public ProtectedSessionStorage SessionStorage { get; }

    /// <summary>
    /// The view model containing live state
    /// </summary>
    public TViewModel ViewModel { get; set; }

    /// <summary>
    /// Computes the initial view model state based on query param values
    /// </summary>
    public TViewModel GetViewModelFromQuery();

    /// <summary>
    /// Translates the <param name="serializable">serializable form of the view model</param> to a relative URL associated
    /// with that state
    /// </summary>
    public (string Path, Dictionary<string, string?>? QueryParameters) GetUrlFromSerializableViewModel(TSerializableViewModel serializable);

    /// <summary>
    /// Maps <see cref="TViewModel"/> to <see cref="TSerializableViewModel"/>, which should contain simple types.
    /// </summary>
    public TSerializableViewModel ConvertViewModelToSerializable();
}

public static class PageExtensions
{
    /// <summary>
    /// Called after a change in the view model that will affect the url associated with new page state
    /// to navigate to the new url and save new state in localstorage.
    /// </summary>
    public static async Task AfterViewModelChangedAsync<TViewModel, TSerializableViewModel>(this IPageWithSessionAndUrlState<TViewModel, TSerializableViewModel> page) where TSerializableViewModel : class
    {
        var serializableViewModel = page.ConvertViewModelToSerializable();
        var pathWithParameters = GetUrlFromPathAndParameterParts(page.GetUrlFromSerializableViewModel(serializableViewModel));

        page.NavigationManager.NavigateTo(pathWithParameters);
        await page.SessionStorage.SetAsync(page.SessionStorageKey, serializableViewModel).ConfigureAwait(false);
    }

    /// <summary>
    /// Called to initialize the page's view model. If <see cref="hasComponentRendered"/> is true,
    /// will try to restore from session storage. Otherwise, it will construct state from the url.
    ///
    /// Because session storage isn't accessible until after component render, this should be called twice:
    /// once after component render, and once after parameters are set.
    /// </summary>
    public static async Task InitializeViewModelAsync<TViewModel, TSerializableViewModel>(this IPageWithSessionAndUrlState<TViewModel, TSerializableViewModel> page, bool hasComponentRendered) where TSerializableViewModel : class
    {
        if (hasComponentRendered && string.Equals(page.BasePath, page.NavigationManager.ToBaseRelativePath(page.NavigationManager.Uri)))
        {
            var result = await page.SessionStorage.GetAsync<TSerializableViewModel>(page.SessionStorageKey).ConfigureAwait(false);
            if (result is { Success: true, Value: not null })
            {
                page.NavigationManager.NavigateTo(GetUrlFromPathAndParameterParts(page.GetUrlFromSerializableViewModel(result.Value)));
                return;
            }
        }

        page.ViewModel = page.GetViewModelFromQuery();
    }

    private static string GetUrlFromPathAndParameterParts((string Path, Dictionary<string, string?>? QueryParameters) parts)
    {
        var (path, queryParameters) = parts;

        return queryParameters is null || queryParameters.Count == 0
            ? path
            : QueryHelpers.AddQueryString(path, queryParameters);
    }
}
