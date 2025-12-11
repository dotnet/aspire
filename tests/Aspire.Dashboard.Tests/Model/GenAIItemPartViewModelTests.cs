// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class GenAIItemPartViewModelTests
{
    [Fact]
    public void CreateMessagePart_ToolCallResponse_StringResponseDecoded()
    {
        // Arrange
        var responsePart = new ToolCallResponsePart
        {
            Response = JsonValue.Create("**Markdown**")
        };

        // Act
        var itemPart = GenAIItemPartViewModel.CreateMessagePart(responsePart);

        // Assert
        Assert.Equal("**Markdown**", itemPart.TextVisualizerViewModel.Text);
        Assert.Equal(DashboardUIHelpers.MarkdownFormat, itemPart.TextVisualizerViewModel.FormatKind);
        Assert.Equal("**Markdown**", itemPart.TextVisualizerViewModel.FormattedText);
    }

    [Fact]
    public void CreateMessagePart_ToolCallResponse_StringResponseContainingJsonFormatted()
    {
        // Arrange
        const string expectedText = """{"answer":42,"unit":"C"}""";
        var responsePart = new ToolCallResponsePart
        {
            Response = JsonValue.Create(expectedText)
        };

        // Act
        var itemPart = GenAIItemPartViewModel.CreateMessagePart(responsePart);

        // Assert
        Assert.Equal(expectedText, itemPart.TextVisualizerViewModel.Text);
        Assert.Equal(DashboardUIHelpers.JsonFormat, itemPart.TextVisualizerViewModel.FormatKind);
        Assert.Equal(
            """
            {
              "answer": 42,
              "unit": "C"
            }
            """,
            itemPart.TextVisualizerViewModel.FormattedText);
    }
}
