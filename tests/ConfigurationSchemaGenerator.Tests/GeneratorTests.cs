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
    [InlineData("T:System.Int32")]
    [InlineData("T:system.int32")]
    [InlineData("T:Azure.Response`1")]
    [InlineData("T:azure.response`1")]
    [InlineData("M:System.Module")]
    [InlineData("M:system.module")]
    [InlineData("M:System.Module`1")]
    [InlineData("M:system.module`1")]
    [InlineData("M:<>__c")]
    [InlineData("M:<>__C")]
    [InlineData("P:<Tracing>k__BackingField")]
    [InlineData("P:Azure.Security.KeyVault.Secrets.KeyVaultSecretIdentifier.VaultUri")]
    [InlineData("P:azure.Security.keyVault.Secrets.KeyVaultSecretIdentifier.VaultUri")]
    public void MatchesXmlDocumentMemberTypePattern(string input)
    {
        Assert.Matches(ConfigSchemaEmitter.XmlDocumentMemberType(), input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no namespace")]
    [InlineData("no-namespace")]
    [InlineData("( abcde )")]
    [InlineData("TM:System.Int32")]
    [InlineData("t:System.Int32")]
    [InlineData("AAAAA:AAAAAA")]
    [InlineData("     A:P    ")]
    [InlineData("https://aka.ms/azsdk/blog/vault-uri")]
    public void DoesNotMatchXmlDocumentMemberTypePattern(string input)
    {
        Assert.DoesNotMatch(ConfigSchemaEmitter.XmlDocumentMemberType(), input);
    }
}
