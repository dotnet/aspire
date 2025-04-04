// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace Aspire.Hosting.Docker.Tests;

public class EnvVarEscaperTests
{
    [Theory]
    [InlineData("$FOO", "$$FOO")]                           // Simple form
    [InlineData("${FOO}", "$${FOO}")]                      // Braced form
    [InlineData("${FOO:-default}", "$${FOO:-default}")]    // Default value
    [InlineData("${ FOO }", "$${FOO}")]                    // Whitespace in braces
    [InlineData("$$FOO", "$$FOO")]                         // Already escaped
    [InlineData("$${FOO}", "$${FOO}")]                     // Already escaped braced
    [InlineData("", "")]                                    // Empty string
    [InlineData("no vars here", "no vars here")]           // No variables
    [InlineData("$FOO$BAR", "$$FOO$$BAR")]                // Multiple variables
    [InlineData("${FOO}${BAR}", "$${FOO}$${BAR}")]        // Multiple braced
    [InlineData("${FOO_BAR}", "$${FOO_BAR}")]             // Underscore in name
    [InlineData("prefix$FOO", "prefix$$FOO")]              // With prefix
    [InlineData("$FOO:5000", "$$FOO:5000")]               // With port number
    public void EscapeUnescapedEnvVars_HandlesVariousPatterns(string input, string expected)
    {
        var result = EscapeUnescapedEnvVars(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("$1FOO")]                      // Starts with number
    [InlineData("${1FOO}")]                    // Braced starts with number
    [InlineData("$FOO-BAR")]                   // Invalid character in simple
    [InlineData("${FOO-BAR}")]                 // Invalid character in braced
    [InlineData("${FOO{BAR}}")]                // Nested braces
    public void EscapeUnescapedEnvVars_IgnoresInvalidNames(string input)
    {
        var result = EscapeUnescapedEnvVars(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("Value is ${FOO:-default}", "Value is $${FOO:-default}")]
    [InlineData("${FOO:-${BAR}}", "$${FOO:-$${BAR}}")]
    [InlineData("${FOO:-$$BAR}", "$${FOO:-$$BAR}")]
    public void EscapeUnescapedEnvVars_HandlesDefaultValues(string input, string expected)
    {
        var result = EscapeUnescapedEnvVars(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("${FOO_$BAR}", "$${FOO_$$BAR}")]
    [InlineData("${FOO_${BAR}}", "$${FOO_$${BAR}}")]
    [InlineData("$FOO_$BAR", "$$FOO_$$BAR")]
    public void EscapeUnescapedEnvVars_HandlesNestedVariables(string input, string expected)
    {
        var result = EscapeUnescapedEnvVars(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EscapeUnescapedEnvVars_ThrowsOnExcessiveInputLength()
    {
        var input = new string('x', 1_000_001);
        var ex = Assert.Throws<ArgumentException>(() => EscapeUnescapedEnvVars(input));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }

    [Fact]
    public void EscapeUnescapedEnvVars_ThrowsOnExcessiveContentLength()
    {
        var content = new string('x', 100_001);
        var input = "${" + content + "}";
        var ex = Assert.Throws<ArgumentException>(() => EscapeUnescapedEnvVars(input));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }

    [Fact]
    public void EscapeUnescapedEnvVars_ThrowsOnExcessiveRecursion()
    {
        var input = "${FOO:-${BAR:-${BAZ:-${QUX:-${AAA:-${BBB:-${CCC:-${DDD:-${EEE:-${FFF:-${GGG:-${HHH:-${III:-${JJJ:-${KKK:-${LLL:-${MMM:-${NNN:-${OOO:-${PPP:-${QQQ:-${RRR:-${SSS:-${TTT:-${UUU:-${VVV:-${WWW:-${XXX:-${YYY:-${ZZZ:-${AAA:-${BBB:-${CCC:-${DDD:-${EEE:-${FFF:-${GGG:-${HHH:-${III:-${JJJ:-${KKK:-${LLL:-${MMM:-${NNN:-${OOO:-${PPP:-${QQQ:-${RRR:-${SSS:-${TTT:-${UUU}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}";
        var ex = Assert.Throws<InvalidOperationException>(() => EscapeUnescapedEnvVars(input));
        Assert.Contains("Maximum recursion depth exceeded", ex.Message);
    }

     /// <summary>
    /// Main entry point for escaping unescaped environment variables in a string.
    /// </summary>
    private static string EscapeUnescapedEnvVars(string input)
    {
        var result = new StringBuilder();
        EnvVarEscaper.EscapeUnescapedEnvVars(input.AsSpan(), result);
        return result.ToString();
    }
}
