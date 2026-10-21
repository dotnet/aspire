// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests.Utils;

internal static class LineEndingAssert
{
    /// <summary>
    /// Asserts that a JSON object document uses LF line breaks only and ends with exactly one newline after
    /// its closing brace, which is how the CLI writes JSON config files on every OS.
    /// </summary>
    public static void IsLfWithSingleFinalNewline(string content)
    {
        Assert.Equal(content.ReplaceLineEndings("\n"), content);
        Assert.EndsWith("}\n", content, StringComparison.Ordinal);
    }

    public static void FileIsLfWithSingleFinalNewline(string path)
    {
        Assert.True(File.Exists(path), $"Expected file at {path}");
        IsLfWithSingleFinalNewline(File.ReadAllText(path));
    }
}
