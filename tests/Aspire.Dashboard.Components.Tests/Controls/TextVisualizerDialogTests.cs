// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Tests.Shared;
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
        var rawJson = """
                      // line comment
                      [
                          /* block comment */
                          1,
                          { "test": {    "nested": "value" } }
                      ]
                      """;

        var expectedJson = """
                           /* line comment*/
                           [
                             /* block comment */
                             1,
                             {
                               "test": {
                                 "nested": "value"
                               }
                             }
                           ]
                           """;

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(rawJson, string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(expectedJson, instance.FormattedText);
        Assert.Equal(TextVisualizerDialog.JsonFormat, instance.FormatKind);
        Assert.Equal([TextVisualizerDialog.JsonFormat, TextVisualizerDialog.PlaintextFormat], instance.EnabledOptions.ToImmutableSortedSet());
    }

    [Fact]
    public async Task Render_TextVisualizerDialog_WithValidXml_FormatsXml_CanChangeFormatAsync()
    {
        const string rawXml = """<parent><child>text<!-- comment --></child></parent>""";
        const string expectedXml =
            """
            <?xml version="1.0" encoding="utf-16"?>
            <parent>
              <child>text<!-- comment --></child>
            </parent>
            """;

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(rawXml, string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(TextVisualizerDialog.XmlFormat, instance.FormatKind);
        Assert.Equal(expectedXml, instance.FormattedText);
        Assert.Equal([TextVisualizerDialog.PlaintextFormat, TextVisualizerDialog.XmlFormat], instance.EnabledOptions.ToImmutableSortedSet());

        // changing format works
        instance.ChangeFormat(TextVisualizerDialog.PlaintextFormat, rawXml);

        Assert.Equal(TextVisualizerDialog.PlaintextFormat, instance.FormatKind);
        Assert.Equal(rawXml, instance.FormattedText);
    }

    [Fact]
    public async Task Render_TextVisualizerDialog_WithValidXml_FormatsXmlWithDoctypeAsync()
    {
        const string rawXml = """<?xml version="1.0" encoding="utf-16"?><test>text content</test>""";
        const string expectedXml =
            """
            <?xml version="1.0" encoding="utf-16"?>
            <test>text content</test>
            """;

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(rawXml, string.Empty), []);

        var instance = cut.FindComponent<TextVisualizerDialog>().Instance;

        Assert.Equal(TextVisualizerDialog.XmlFormat, instance.FormatKind);
        Assert.Equal(expectedXml, instance.FormattedText);
        Assert.Equal([TextVisualizerDialog.PlaintextFormat, TextVisualizerDialog.XmlFormat], instance.EnabledOptions.ToImmutableSortedSet());
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

    [Fact]
    public async Task Render_TextVisualizerDialog_WithDifferentThemes_LineClassesChange()
    {
        var xml = @"<hello><!-- world --></hello>";
        var themeManager = new ThemeManager(new TestThemeResolver { EffectiveTheme = "Light" });
        var cut = SetUpDialog(out var dialogService, themeManager: themeManager);
        themeManager.EffectiveTheme = "Light";
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(xml, string.Empty), []);

        Assert.NotEmpty(cut.FindAll(".theme-a11y-light-min"));

        themeManager.EffectiveTheme = "Dark";
        var instance = cut.FindComponent<TextVisualizerDialog>();
        instance.Render();

        Assert.NotEmpty(cut.FindAll(".theme-a11y-dark-min"));
    }

    [Fact]
    public async Task Render_TextVisualizerDialog_ResolveTheme_LineClassesChange()
    {
        var xml = @"<hello><!-- world --></hello>";

        var cut = SetUpDialog(out var dialogService);
        await dialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(xml, string.Empty), []);

        Assert.NotEmpty(cut.FindAll(".theme-a11y-dark-min"));
    }

    private IRenderedFragment SetUpDialog(out IDialogService dialogService, ThemeManager? themeManager = null)
    {
        themeManager ??= new ThemeManager(new TestThemeResolver());

        Services.AddFluentUIComponents();
        Services.AddSingleton(themeManager);
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddLocalization();
        var module = JSInterop.SetupModule("/Components/Dialogs/TextVisualizerDialog.razor.js");
        module.SetupVoid();

        var cut = Render(builder =>
        {
            builder.OpenComponent<FluentDialogProvider>(0);
            builder.CloseComponent();
        });

        // Setting a provider ID on menu service is required to simulate <FluentMenuProvider> on the page.
        // This makes FluentMenu render without error.
        var menuService = Services.GetRequiredService<IMenuService>();
        menuService.ProviderId = "Test";

        dialogService = Services.GetRequiredService<IDialogService>();
        return cut;
    }
}
