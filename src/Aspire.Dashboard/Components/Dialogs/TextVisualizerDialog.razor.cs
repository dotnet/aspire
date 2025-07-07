// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class TextVisualizerDialog : ComponentBase, IAsyncDisposable
{
    // xml and json are language names supported by highlight.js
    public const string XmlFormat = "xml";
    public const string JsonFormat = "json";
    public const string PlaintextFormat = "plaintext";

    private readonly string _copyButtonId = $"copy-{Guid.NewGuid():N}";
    private readonly string _openSelectFormatButtonId = $"select-format-{Guid.NewGuid():N}";
    private readonly string _logContainerId = $"log-container-{Guid.NewGuid():N}";

    private IJSObjectReference? _jsModule;
    private List<SelectViewModel<string>> _options = null!;
    private string? _currentValue;
    private string _formattedText = string.Empty;
    private bool _isLoading = true;

    public HashSet<string?> EnabledOptions { get; } = [];
    internal bool? ShowSecretsWarning { get; private set; }

    public string FormattedText
    {
        get => _formattedText;
        private set
        {
            _formattedText = value;
            FormattedLines = GetLines();
        }
    }

    public ICollection<StringLogLine> FormattedLines { get; set; } = [];

    public string FormatKind { get; private set; } = PlaintextFormat;

    [Parameter, EditorRequired]
    public required TextVisualizerDialogViewModel Content { get; set; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required ILocalStorage LocalStorage { get; init; }

    protected override async Task OnInitializedAsync()
    {
        await ThemeManager.EnsureInitializedAsync();

        // We need to make users perform an explicit action once before being able to see secret values
        // We do this by making them agree to a warning in the text visualizer dialog.
        if (Content.ContainsSecret)
        {
            var settingsResult = await LocalStorage.GetUnprotectedAsync<TextVisualizerDialogSettings>(BrowserStorageKeys.TextVisualizerDialogSettings);
            ShowSecretsWarning = settingsResult.Value is not { SecretsWarningAcknowledged: true };
        }

        // Don't display content until it is loaded.
        // This is required because rendering uses the theme manager, and we don't want to call that code until we know it's finished initializing.
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Dialogs/TextVisualizerDialog.razor.js");
        }

        if (_jsModule is not null && IsTextContentDisplayed)
        {
            if (FormatKind is not PlaintextFormat)
            {
                await _jsModule.InvokeVoidAsync("connectObserver", _logContainerId);
            }
            else
            {
                await _jsModule.InvokeVoidAsync("disconnectObserver", _logContainerId);
            }
        }
    }

    protected override void OnParametersSet()
    {
        EnabledOptions.Clear();
        EnabledOptions.Add(PlaintextFormat);

        _options = [
            new SelectViewModel<string> { Id = PlaintextFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogPlaintextFormat)] },
            new SelectViewModel<string> { Id = JsonFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogJsonFormat)] },
            new SelectViewModel<string> { Id = XmlFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogXmlFormat)] }
        ];

        if (TryFormatJson())
        {
            EnabledOptions.Add(JsonFormat);
        }
        else if (TryFormatXml())
        {
            EnabledOptions.Add(XmlFormat);
        }
        else
        {
            ChangeFormattedText(PlaintextFormat, Content.Text);
        }
    }

    private bool IsTextContentDisplayed
    {
        get
        {
            if (_isLoading)
            {
                return false;
            }

            return !Content.ContainsSecret || ShowSecretsWarning is false;
        }
    }

    private string GetLogContentClass()
    {
        // we support light (a11y-light-min) and dark (a11y-dark-min) themes. syntax to force a theme for highlight.js
        // is "theme-{themeName}"
        return $"log-content highlight-line language-{FormatKind} theme-a11y-{ThemeManager.EffectiveTheme.ToLower()}-min";
    }

    private ICollection<StringLogLine> GetLines()
    {
        var lines = FormattedText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();

        return lines.Select((line, index) => new StringLogLine(index + 1, line, FormatKind != PlaintextFormat)).ToList();
    }

    private bool TryFormatXml()
    {
        try
        {
            var document = XDocument.Parse(Content.Text);
            var stringWriter = new StringWriter();
            document.Save(stringWriter);
            ChangeFormattedText(XmlFormat, stringWriter.ToString());
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private bool TryFormatJson()
    {
        try
        {
            var formattedJson = FormatJson(Content.Text);
            ChangeFormattedText(JsonFormat, formattedJson);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void OnFormatOptionChanged(MenuChangeEventArgs args) => ChangeFormat(args.Id, args.Value);

    public void ChangeFormat(string? newFormat, string? value)
    {
        _currentValue = value;

        if (newFormat == XmlFormat)
        {
            TryFormatXml();
        }
        else if (newFormat == JsonFormat)
        {
            TryFormatJson();
        }
        else
        {
            ChangeFormattedText(PlaintextFormat, Content.Text);
        }
    }

    private void ChangeFormattedText(string newFormatKind, string newText)
    {
        FormatKind = newFormatKind;
        FormattedText = newText;
    }

    private static string FormatJson(string jsonString)
    {
        var jsonData = Encoding.UTF8.GetBytes(jsonString);

        // Initialize the Utf8JsonReader
        var reader = new Utf8JsonReader(jsonData, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Allow,
            // Increase the allowed limit to 1000. This matches the allowed limit of the writer.
            // It's ok to allow recursion here because JSON is read in a flat loop. There isn't a danger
            // of recursive method calls that cause a stack overflow.
            MaxDepth = 1000
        });

        // Use a MemoryStream and Utf8JsonWriter to write the formatted JSON
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString()!);
                    break;
                case JsonTokenType.String:
                    writer.WriteStringValue(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var intValue))
                    {
                        writer.WriteNumberValue(intValue);
                    }
                    else if (reader.TryGetDouble(out var doubleValue))
                    {
                        writer.WriteNumberValue(doubleValue);
                    }
                    break;
                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;
                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
                case JsonTokenType.Comment:
                    writer.WriteCommentValue(reader.GetComment());
                    break;
            }
        }

        writer.Flush();
        var formattedJson = Encoding.UTF8.GetString(stream.ToArray());

        return formattedJson;
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("disconnectObserver");
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
                // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
            }
        }
    }

    public record StringLogLine(int LineNumber, string Content, bool IsFormatted);

    public static async Task OpenDialogAsync(ViewportInformation viewportInformation, IDialogService dialogService,
        IStringLocalizer<Resources.Dialogs> dialogsLoc, string valueDescription, string value, bool containsSecret)
    {
        var width = viewportInformation.IsDesktop ? "75vw" : "100vw";
        var parameters = new DialogParameters
        {
            Title = valueDescription,
            DismissTitle = dialogsLoc[nameof(Resources.Dialogs.DialogCloseButtonText)],
            Width = $"min(1000px, {width})",
            TrapFocus = true,
            Modal = true,
            PreventScroll = true,
        };

        await dialogService.ShowDialogAsync<TextVisualizerDialog>(
            new TextVisualizerDialogViewModel(value, valueDescription, containsSecret), parameters);
    }

    internal sealed record TextVisualizerDialogSettings(bool SecretsWarningAcknowledged);

    private async Task UnmaskContentAsync()
    {
        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.TextVisualizerDialogSettings, new TextVisualizerDialogSettings(SecretsWarningAcknowledged: true));
        ShowSecretsWarning = false;
    }
}

