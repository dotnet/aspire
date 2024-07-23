// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class TextVisualizerDialog : ComponentBase
{
    private const string XmlFormat = "xml";
    private const string JsonFormat = "json";
    private const string PlaintextFormat = "text";

    private List<SelectViewModel<string>> _options = null!;
    private string _selectedOption = null!;

    private string _formattedText = string.Empty;

    private readonly string _openSelectFormatButtonId = $"select-format-{Guid.NewGuid():N}";
    private bool _isSelectFormatPopupOpen;

    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    protected override void OnInitialized()
    {
        _options =
        [
            new SelectViewModel<string> { Id = PlaintextFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogPlaintextFormat)] },
            new SelectViewModel<string> { Id = JsonFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogJsonFormat)] },
            new SelectViewModel<string> { Id = XmlFormat, Name = Loc[nameof(Resources.Dialogs.TextVisualizerDialogXmlFormat)] },
        ];
    }

    protected override void OnParametersSet()
    {
        // We don't know what format the string is in, but we can guess
        if (TryFormatXml())
        {
            _selectedOption = XmlFormat;
            return;
        }

        if (TryFormatJson())
        {
            _selectedOption = JsonFormat;
            return;
        }

        _selectedOption = PlaintextFormat;
        _formattedText = Content.Text;
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
    }
}

