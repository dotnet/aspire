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

    [Theory]
    [InlineData("", "")]
    [InlineData("no namespace", "no namespace")]
    [InlineData("no-namespace", "no-namespace")]
    [InlineData("T:System.Uri", "'System.Uri'")]
    [InlineData("T:Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier", "'Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier'")]
    [InlineData("P:Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier.VaultUri", "'Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier.VaultUri'")]
    [InlineData("https://aka.ms/azsdk/blog/vault-uri", "https://aka.ms/azsdk/blog/vault-uri")]
    public void ShouldTrimAndSanitize(string input, string expected)
    {
        var result = ConfigSchemaEmitter.ReplaceMemberTypePrefixIfNecessary(input);

        Assert.Equal(expected, result);
    }
}
