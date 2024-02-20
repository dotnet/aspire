// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests.Utils;

public class PasswordGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public PasswordGeneratorTests(ITestOutputHelper output)
    {
        this._output = output;
    }

    [Fact]
    public void ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(0, 0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(0, 0, 0, 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void IncludesLowerCase(int length)
    {
        var password = PasswordGenerator.GeneratePassword(length, 0, 0, 0);

        Assert.Equal(length, password.Length);
        Assert.True(password.All(PasswordGenerator.LowerCaseChars.Contains));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void IncludesUpperCase(int length)
    {
        var password = PasswordGenerator.GeneratePassword(0, length, 0, 0);

        Assert.Equal(length, password.Length);
        Assert.True(password.All(PasswordGenerator.UpperCaseChars.Contains));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void IncludesDigit(int length)
    {
        var password = PasswordGenerator.GeneratePassword(0, 0, length, 0);

        Assert.Equal(length, password.Length);
        Assert.True(password.All(PasswordGenerator.DigitChars.Contains));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void IncludesSpecial(int length)
    {
        var password = PasswordGenerator.GeneratePassword(0, 0, 0, length);

        Assert.Equal(length, password.Length);
        Assert.True(password.All(PasswordGenerator.SpecialChars.Contains));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void IncludesAll(int length)
    {
        var password = PasswordGenerator.GeneratePassword(length, length, length, length);

        Assert.Equal(length * 4, password.Length);
        Assert.Equal(length, password.Count(PasswordGenerator.LowerCaseChars.Contains));
        Assert.Equal(length, password.Count(PasswordGenerator.UpperCaseChars.Contains));
        Assert.Equal(length, password.Count(PasswordGenerator.DigitChars.Contains));
        Assert.Equal(length, password.Count(PasswordGenerator.SpecialChars.Contains));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void ValidUriCharacters(int length)
    {
        var password = PasswordGenerator.GeneratePassword(length, length, length, length);
        var fakeUri = new Uri($"https://guest:{password}@localhost:12345");

        _output.WriteLine($"Generated password: {password}");
        _output.WriteLine($"Fake URI: {fakeUri.OriginalString}");

        Assert.Equal(length * 4, password.Length);

        // validate that the password contains only valid URI characters
        Assert.True(Uri.IsWellFormedUriString(fakeUri.AbsoluteUri, UriKind.Absolute));
    }
}
