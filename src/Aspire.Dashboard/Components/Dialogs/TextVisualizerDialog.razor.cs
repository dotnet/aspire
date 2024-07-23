// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class TextVisualizerDialog : ComponentBase
{
    private const string XmlFormat = "xml";
    private const string JsonFormat = "json";
    private const string PlaintextFormat = "text";
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    private List<SelectViewModel<string>> _options = null!;
    private readonly HashSet<string?> _enabledOptions = [];
    private string _selectedOption = null!;
    private string _formattedText = string.Empty;

    private readonly string _openSelectFormatButtonId = $"select-format-{Guid.NewGuid():N}";
    private bool _isSelectFormatPopupOpen;

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
            _selectedOption = JsonFormat;
            _enabledOptions.Add(JsonFormat);
        }
        else if (TryFormatXml())
        {
            _selectedOption = XmlFormat;
            _enabledOptions.Add(XmlFormat);
        }
        else
        {
            _selectedOption = PlaintextFormat;
            _formattedText = Content.Text;
        }
    }

    private ICollection<ResourceLogLine> GetLines()
    {
        var lines = Regex.Split(_formattedText, "(\r\n|\n)", RegexOptions.Compiled).ToList();
        if (lines.Count > 0 && lines[0].Length == 0)
        {
            lines.RemoveAt(0);
        }

        return lines.Select((line, index) => new ResourceLogLine(index, line, false)).ToList();
    }

    private bool TryFormatXml()
    {
        try
        {
            _formattedText = XDocument.Parse(Content.Text).ToString();
            return true;
        }
        catch (XmlException)
        {
            // If the XML is invalid, just show the original text
            _formattedText = Content.Text;
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
                    CommentHandling = JsonCommentHandling.Skip
                }
            );

            _formattedText = JsonSerializer.Serialize(doc.RootElement, s_serializerOptions);
            return true;
        }
        catch (JsonException)
        {
            // If the JSON is invalid, just show the original text
            _formattedText = Content.Text;
            return false;
        }
    }

    private void OnFormatOptionChanged()
    {
        if (_selectedOption == XmlFormat)
        {
            TryFormatXml();
        }
        else if (_selectedOption == JsonFormat)
        {
            TryFormatJson();
        }
        else
        {
            _formattedText = Content.Text;
        }

        _isSelectFormatPopupOpen = false;
    }
}

