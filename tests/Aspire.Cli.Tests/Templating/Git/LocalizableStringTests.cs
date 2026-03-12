// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

public class LocalizableStringTests
{
    #region Plain string

    [Fact]
    public void FromString_ResolvesOriginalValue()
    {
        var ls = LocalizableString.FromString("hello");
        Assert.Equal("hello", ls.Resolve());
    }

    [Fact]
    public void FromString_EmptyString_ResolvesEmpty()
    {
        var ls = LocalizableString.FromString("");
        Assert.Equal("", ls.Resolve());
    }

    [Fact]
    public void ImplicitConversion_StringToLocalizableString_Works()
    {
        LocalizableString ls = "implicit value";
        Assert.Equal("implicit value", ls.Resolve());
    }

    [Fact]
    public void ToString_ReturnsResolvedValue()
    {
        var ls = LocalizableString.FromString("test");
        Assert.Equal("test", ls.ToString());
    }

    #endregion

    #region Localized object

    [Fact]
    public void FromLocalizations_ExactCultureMatch_ResolvesCorrectly()
    {
        var ls = LocalizableString.FromLocalizations(new Dictionary<string, string>
        {
            ["en"] = "English",
            ["de"] = "Deutsch"
        });

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de");
            Assert.Equal("Deutsch", ls.Resolve());
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
    }

    [Fact]
    public void FromLocalizations_ParentCultureFallback_Works()
    {
        var ls = LocalizableString.FromLocalizations(new Dictionary<string, string>
        {
            ["en"] = "English",
            ["de"] = "Deutsch"
        });

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            // "en-US" should fall back to "en"
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal("English", ls.Resolve());
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
    }

    [Fact]
    public void FromLocalizations_NoMatch_FallsBackToFirstEntry()
    {
        var ls = LocalizableString.FromLocalizations(new Dictionary<string, string>
        {
            ["en"] = "English",
            ["de"] = "Deutsch"
        });

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ja");
            // Should fall back to first entry
            var result = ls.Resolve();
            Assert.True(result == "English" || result == "Deutsch",
                $"Expected fallback to first entry, got '{result}'");
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
    }

    [Fact]
    public void FromLocalizations_EmptyDictionary_ReturnsEmpty()
    {
        var ls = LocalizableString.FromLocalizations([]);
        Assert.Equal("", ls.Resolve());
    }

    [Fact]
    public void FromLocalizations_CaseInsensitiveKeys_Works()
    {
        var ls = LocalizableString.FromLocalizations(new Dictionary<string, string>
        {
            ["EN"] = "English"
        });

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            Assert.Equal("English", ls.Resolve());
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
    }

    #endregion

    #region JSON deserialization

    [Fact]
    public void Deserialize_PlainString_CreatesLocalizableString()
    {
        var json = """{"displayName": "My Template"}""";
        var result = JsonSerializer.Deserialize<TestLocalizableHolder>(json, s_jsonOptions);
        Assert.NotNull(result?.DisplayName);
        Assert.Equal("My Template", result.DisplayName.Resolve());
    }

    [Fact]
    public void Deserialize_LocalizedObject_CreatesLocalizableString()
    {
        var json = """{"displayName": {"en": "English Name", "de": "Deutscher Name"}}""";
        var result = JsonSerializer.Deserialize<TestLocalizableHolder>(json, s_jsonOptions);

        Assert.NotNull(result?.DisplayName);

        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de");
            Assert.Equal("Deutscher Name", result.DisplayName.Resolve());
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
        }
    }

    [Fact]
    public void Deserialize_NullValue_ReturnsNull()
    {
        var json = """{"displayName": null}""";
        var result = JsonSerializer.Deserialize<TestLocalizableHolder>(json, s_jsonOptions);
        Assert.Null(result?.DisplayName);
    }

    [Fact]
    public void Deserialize_MissingField_ReturnsNull()
    {
        var json = """{}""";
        var result = JsonSerializer.Deserialize<TestLocalizableHolder>(json, s_jsonOptions);
        Assert.Null(result?.DisplayName);
    }

    [Fact]
    public void Serialize_PlainString_WritesString()
    {
        var holder = new TestLocalizableHolder { DisplayName = "Test" };
        var json = JsonSerializer.Serialize(holder, s_jsonOptions);
        Assert.Contains("\"Test\"", json);
    }

    private sealed class TestLocalizableHolder
    {
        public LocalizableString? DisplayName { get; set; }
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #endregion
}
