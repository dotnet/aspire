// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

internal static class DashboardUIHelpers
{
    public const string MessageBarSection = "MessagesTop";

    // these are language names supported by highlight.js
    public const string XmlFormat = "xml";
    public const string JsonFormat = "json";
    public const string JavascriptFormat = "javascript";
    public const string PlaintextFormat = "plaintext";
    public const string MarkdownFormat = "markdown";

    // The initial data fetch for a FluentDataGrid doesn't include a count of items to return.
    // The data grid doesn't specify a count because it doesn't know how many items fit in the UI.
    // Once it knows the height of items and the height of the grid then it specifies the desired item count
    // and then virtualization will fetch more data as needed. The problem with this is the initial fetch
    // could fetch all available data when it doesn't need to.
    //
    // If there is no count then default to a limit to avoid getting all data.
    // Given the size of rows on dashboard grids, 100 rows should always fill the grid on the screen.
    public const int DefaultDataGridResultCount = 100;

    // Don't attempt to display more than 2 highlighted commands. Many commands will take up too much space.
    public const int MaxHighlightedCommands = 2;

    public static readonly TimeSpan ToastTimeout = TimeSpan.FromMilliseconds(5000);

    public static (ColumnResizeLabels resizeLabels, ColumnSortLabels sortLabels) CreateGridLabels(IStringLocalizer<ControlsStrings> loc)
    {
        var resizeLabels = ColumnResizeLabels.Default with
        {
            ExactLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeLabel)],
            ResizeMenu = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeButtonText)],
            DiscreteLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeDiscreteLabel)],
            GrowAriaLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellGrowAriaLabelText)],
            ResetAriaLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResetAriaLabelText)],
            ShrinkAriaLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellShrinkAriaLabelText)],
            SubmitAriaLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSubmitAriaLabelText)]
        };
        var sortLabels = ColumnSortLabels.Default with
        {
            SortMenu = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortButtonText)],
            SortMenuAscendingLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortAscendingButtonText)],
            SortMenuDescendingLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortDescendingButtonText)]
        };
        return (resizeLabels, sortLabels);
    }

    private static readonly ConcurrentDictionary<int, TextMask> s_cachedMasking = new();

    public static TextMask GetMaskingText(int length)
    {
        return s_cachedMasking.GetOrAdd(length, static i =>
        {
            const string markupMaskingChar = "&#x25cf;";
            const string textMaskingChar = "●";

            return new TextMask(
                new MarkupString(Repeat(markupMaskingChar, i)),
                Repeat(textMaskingChar, i)
            );

            static string Repeat(string s, int n) => new StringBuilder(s.Length * n)
                .Insert(0, s, n)
                .ToString();
        });
    }

    public static async Task<Message> DisplayMaxLimitMessageAsync(IMessageService messageService, string title, string message, Action onClose)
    {
        return await messageService.ShowMessageBarAsync(options =>
        {
            options.Title = title;
            options.Body = message;
            options.Intent = MessageIntent.Info;
            options.Section = "MessagesTop";
            options.AllowDismiss = true;
            options.OnClose = m =>
            {
                onClose();
                return Task.CompletedTask;
            };
        }).ConfigureAwait(false);
    }

    public static bool TryGetAsset(ComponentBase component, string path, [NotNullWhen(true)] out object? asset)
    {
        var assetProperty = typeof(ComponentBase).GetProperty("Assets", BindingFlags.NonPublic | BindingFlags.Instance);

        if (assetProperty != null)
        {
            var assets = assetProperty.GetValue(component);
            if (assets != null)
            {
                // Find the indexer property (default property with string parameter)
                var indexer = assets.GetType().GetProperty("Item", types: [typeof(string)]);

                if (indexer != null)
                {
                    asset = indexer.GetValue(assets, [path]);
                    return asset != null;
                }
            }
        }

        asset = null;
        return false;
    }

    public static void CallMapStaticAssets(IEndpointRouteBuilder endpoints)
    {
        //Debugger.Launch();

        Assembly.Load("Microsoft.AspNetCore.StaticAssets");

        // 1. Find the assembly containing StaticAssetsEndpointRouteBuilderExtensions
        //    (it may vary depending on SDK version, often "Microsoft.AspNetCore.StaticAssets" or similar)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        Type? extensionsType = null;
        foreach (var asm in assemblies)
        {
            if (asm.GetName().Name == "Microsoft.AspNetCore.StaticAssets")
            {
                var allTypes = asm.GetTypes();
                _ = allTypes;
            }
            extensionsType = asm.GetType("Microsoft.AspNetCore.Builder.StaticAssetsEndpointRouteBuilderExtensions");
            if (extensionsType != null)
            {
                Console.WriteLine($"Found extensions in assembly: {asm.FullName}");
                break;
            }
        }

        if (extensionsType == null)
        {
            return;
        }

        // 2. Find the MapStaticAssets method
        var method = extensionsType.GetMethod(
            "MapStaticAssets",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(IEndpointRouteBuilder), typeof(string) }, // overload resolution
            modifiers: null
        );

        if (method == null)
        {
            return;
        }

        // 3. Invoke the method
        method.Invoke(null, [endpoints, null]);
    }
}

internal record TextMask(MarkupString MarkupString, string Text);
