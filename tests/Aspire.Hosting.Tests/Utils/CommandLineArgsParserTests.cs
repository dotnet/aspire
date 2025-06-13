// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.Hosting.Utils.CommandLineArgsParser;
using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class CommandLineArgsParserTests
{
    [Theory]
    [InlineData("", new string[] { })]
    [InlineData("single", new[] { "single" })]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("foo bar baz", new[] { "foo", "bar", "baz" })]
    [InlineData("foo\tbar\tbaz", new[] { "foo", "bar", "baz" })]
    [InlineData("\"quoted string\"", new[] { "quoted string" })]
    [InlineData("\"quoted\tstring\"", new[] { "quoted\tstring" })]
    [InlineData("\"quoted \"\" string\"", new[] { "quoted \" string" })]
    // Single quotes are not treated as string delimiters
    [InlineData("\"hello 'world'\"", new[] { "hello 'world'" })]
    [InlineData("'single quoted'", new[] { "'single", "quoted'" })]
    [InlineData("'foo \"bar\" baz'", new[] { "'foo", "bar", "baz'" })]
    public void TestParse(string commandLine, string[] expectedParsed)
    {
        var actualParsed = Parse(commandLine);

        Assert.Equal(expectedParsed, actualParsed.ToArray());
    }
}
