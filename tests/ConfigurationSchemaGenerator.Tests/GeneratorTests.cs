// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
}
