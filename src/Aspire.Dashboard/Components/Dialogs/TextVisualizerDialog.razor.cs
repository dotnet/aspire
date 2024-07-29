// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class TextVisualizerDialog : ComponentBase, IAsyncDisposable
{
    private const string XmlFormat = "xml";
    private const string JsonFormat = "json";
    private const string PlaintextFormat = "plaintext";
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    private List<SelectViewModel<string>> _options = null!;
    private readonly HashSet<string?> _enabledOptions = [];
    private string _formattedText = string.Empty;
    private string? _formatKind;
    private readonly string _copyButtonId = $"copy-{Guid.NewGuid():N}";

    private readonly string _openSelectFormatButtonId = $"select-format-{Guid.NewGuid():N}";
    private IJSObjectReference? _jsModule;

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Dialogs/TextVisualizerDialog.razor.js");
        }

        if (_jsModule is not null && _formatKind is not null)
        {
            await _jsModule.InvokeVoidAsync("connectObserver");
        }
    }

    protected override void OnParametersSet()
    {
        _enabledOptions.Clear();
        _enabledOptions.Add(PlaintextFormat);

        _options = [
            new SelectViewModel<string> { Id = PlaintextFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogPlaintextFormat)] },
            new SelectViewModel<string> { Id = JsonFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogJsonFormat)] },
            new SelectViewModel<string> { Id = XmlFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogXmlFormat)] }
        ];

        if (TryFormatJson())
        {
            _enabledOptions.Add(JsonFormat);
        }
        else if (TryFormatXml())
        {
            _enabledOptions.Add(XmlFormat);
        }
        else
        {
            _formattedText = Content.Text;
            _formatKind = null;
        }
    }

    private string GetLogContentClass()
    {
        return $"log-content highlight-line language-{_formatKind}";
    }

    private ICollection<StringLogLine> GetLines()
    {
        var lines = Regex.Split(_formattedText, Environment.NewLine, RegexOptions.Compiled).ToList();
        if (lines.Count > 0 && lines[0].Length == 0)
        {
            lines.RemoveAt(0);
        }

        return lines.Select((line, index) => new StringLogLine(index, line, _formatKind is not null)).ToList();
    }

    private bool TryFormatXml()
    {
        try
        {
            _formattedText = XDocument.Parse(Content.Text).ToString();
            _formatKind = XmlFormat;
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
            using var doc = JsonDocument.Parse(
                Content.Text,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    /* comments are not allowed in JSON, the only options are Skip or Disallow. Skip to allow showing formatted JSON in any case */
                    CommentHandling = JsonCommentHandling.Skip
                }
            );

            _formattedText = JsonSerializer.Serialize(doc.RootElement, s_serializerOptions);
            _formatKind = JsonFormat;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void OnFormatOptionChanged(MenuChangeEventArgs args)
    {
        if (args.Id == XmlFormat)
        {
            TryFormatXml();
        }
        else if (args.Id == JsonFormat)
        {
            TryFormatJson();
        }
        else
        {
            _formattedText = Content.Text;
            _formatKind = null;
        }
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

    private record StringLogLine(int LineNumber, string Content, bool IsFormatted);
}

