// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

public class TextVisualizerDialogTests : TestContext
{
    [Fact]
    public async Task Render_TextVisualizerDialog_WithValidJson_FormatsJsonAsync()
    {
        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel("""{"test": 4}""", string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(TextVisualizerDialog.JsonFormat, instance.FormatKind);
        Assert.Equal([TextVisualizerDialog.JsonFormat, TextVisualizerDialog.PlaintextFormat], instance.EnabledOptions.ToImmutableSortedSet());
    }

    [Fact]
    public async Task Render_TextVisualizerDialog_WithValidXml_FormatsXml_CanChangeFormatAsync()
    {
        const string rawXml = """<parent><child>text<!-- comment --></child></parent>""";

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(rawXml, string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(TextVisualizerDialog.XmlFormat, instance.FormatKind);
        Assert.NotEqual(rawXml, instance.FormattedText);
        Assert.Equal([TextVisualizerDialog.PlaintextFormat, TextVisualizerDialog.XmlFormat], instance.EnabledOptions.ToImmutableSortedSet());

        // changing format works
        instance.ChangeFormat(TextVisualizerDialog.PlaintextFormat);

        Assert.Equal(TextVisualizerDialog.PlaintextFormat, instance.FormatKind);
        Assert.Equal(rawXml, instance.FormattedText);
    }

    [Fact]
    public async Task Render_TextVisualizerDialog_WithInvalidJson_FormatsPlaintextAsync()
    {
        const string rawText = """{{{{{{"test": 4}""";

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(rawText, string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(TextVisualizerDialog.PlaintextFormat, instance.FormatKind);
        Assert.Equal(rawText, instance.FormattedText);
        Assert.Equal([TextVisualizerDialog.PlaintextFormat], instance.EnabledOptions.ToImmutableSortedSet());
    }

    private IRenderedFragment SetUpDialog(out IDialogService dialogService)
    {
        Services.AddFluentUIComponents();
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddLocalization();
        var module = JSInterop.SetupModule("/Components/Dialogs/TextVisualizerDialog.razor.js");
        module.SetupVoid("connectObserver");

        var cut = Render(builder =>
        {
            builder.OpenComponent<FluentDialogProvider>(0);
            builder.CloseComponent();
        });

        dialogService = Services.GetRequiredService<IDialogService>();
        return cut;
    }
}
