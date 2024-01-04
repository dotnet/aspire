// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace ConfigurationSchemaGenerator.Tests;

public class GeneratorTests
{
    [Theory]
    [InlineData("abc\n  def", "abc\ndef")]
    [InlineData("\n  def", "\ndef")]
    [InlineData(" \n  def", "\ndef")]
    [InlineData("  \n  def", "\ndef")]
    [InlineData("abc\n", "abc\n")]
    [InlineData("abc\n ", "abc\n")]
    [InlineData("abc\n  ", "abc\n")]
    [InlineData("abc\n\n  ", "abc\n\n")]
    [InlineData("\n\n  def", "\n\ndef")]
    [InlineData("abc\n def  \n ghi", "abc\ndef\nghi")]
    [InlineData("abc\r\n  def", "abc\ndef")]
    [InlineData("\r\n  def", "\ndef")]
    [InlineData(" \r\n  def", "\ndef")]
    [InlineData("  \r\n  def", "\ndef")]
    [InlineData("abc\r\n", "abc\n")]
    [InlineData("abc\r\n ", "abc\n")]
    [InlineData("abc\r\n  ", "abc\n")]
    [InlineData("abc\r\n\r\n  ", "abc\n\n")]
    [InlineData("\r\n\r\n  def", "\n\ndef")]
    [InlineData("abc\r\n def  \r\n ghi", "abc\ndef\nghi")]
    public void ShouldRemoveIndentation(string value, string expected)
    {
        var builder = new StringBuilder();

        ConfigSchemaEmitter.AppendUnindentedValue(builder, value);

        Assert.Equal(expected, builder.ToString());
    }

    [Theory]
    [InlineData("<see cref=\"N:System.Diagnostics\"/>", "'System.Diagnostics'")]
    [InlineData("<see cref=\"T:System.Uri\"/>", "'System.Uri'")]
    [InlineData("<see cref=\"T:Azure.Core.Extensions.IAzureClientBuilder`2\"/>", "'Azure.Core.Extensions.IAzureClientBuilder`2'")]
    [InlineData("<see cref=\"F:Azure.Storage.Queues.QueueMessageEncoding.None\"/>", "'Azure.Storage.Queues.QueueMessageEncoding.None'")]
    [InlineData("<see cref=\"P:Aspire.Azure.Storage.Blobs.AzureStorageBlobsSettings.ConnectionString\"/>", "'Aspire.Azure.Storage.Blobs.AzureStorageBlobsSettings.ConnectionString'")]
    [InlineData("<see cref=\"M:System.Diagnostics.Debug.Assert(bool)\"/>", "'System.Diagnostics.Debug.Assert(bool)'")]
    [InlineData("<see cref=\"E:System.Windows.Input.ICommand.CanExecuteChanged\"/>", "'System.Windows.Input.ICommand.CanExecuteChanged'")]
    [InlineData("<exception cref=\"T:System.InvalidOperationException\" />", "'System.InvalidOperationException'")]
    public void StripXmlElementsShouldFormatCrefAttributes(string input, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        using Stream stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream);
        reader.MoveToContent();
        var node = (XElement)XNode.ReadFrom(reader);
        var stripedNodes = ConfigSchemaEmitter.StripXmlElements(node);

        var first = stripedNodes.First();
        Assert.Equal(expected, first.ToString().Trim());
    }

    [Theory]
    [InlineData("<see href=\"https://aka.ms/azsdk/blog/vault-uri\"/>", "https://aka.ms/azsdk/blog/vault-uri")]
    [InlineData("<see langword=\"true\"/>", "true")]
    public void StripXmlElementsShouldNotFormatNonCrefAttributes(string input, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        using Stream stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream);
        reader.MoveToContent();
        var node = (XElement)XNode.ReadFrom(reader);
        var stripedNodes = ConfigSchemaEmitter.StripXmlElements(node);

        var first = stripedNodes.First();
        Assert.Equal(expected, first.ToString().Trim());
    }

    [Theory]
    [InlineData("<param name=\"connectionName\">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>", "A name used to retrieve the connection string from the ConnectionStrings configuration section.")]
    [InlineData("<paramref name=\"name\"/>", "name")]
    public void StripXmlElementsShouldNotFormatMiscAttributes(string input, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        using Stream stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream);
        reader.MoveToContent();
        var node = (XElement)XNode.ReadFrom(reader);
        var stripedNodes = ConfigSchemaEmitter.StripXmlElements(node);

        var first = stripedNodes.First();
        Assert.Equal(expected, first.ToString().Trim());
    }
}
