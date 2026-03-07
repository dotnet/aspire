// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

public class TemplateExpressionEvaluatorTests
{
    #region Basic substitution

    [Fact]
    public void Evaluate_SimpleVariable_SubstitutesValue()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("Hello {{name}}", variables);
        Assert.Equal("Hello MyApp", result);
    }

    [Fact]
    public void Evaluate_MultipleVariables_SubstitutesAll()
    {
        var variables = new Dictionary<string, string>
        {
            ["first"] = "Hello",
            ["second"] = "World"
        };
        var result = TemplateExpressionEvaluator.Evaluate("{{first}} {{second}}", variables);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Evaluate_SameVariableMultipleTimes_SubstitutesAll()
    {
        var variables = new Dictionary<string, string> { ["name"] = "App" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name}}.AppHost/{{name}}.csproj", variables);
        Assert.Equal("App.AppHost/App.csproj", result);
    }

    [Fact]
    public void Evaluate_UnresolvedVariable_LeftAsIs()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name}} {{unknown}}", variables);
        Assert.Equal("MyApp {{unknown}}", result);
    }

    [Fact]
    public void Evaluate_NoExpressions_ReturnsInputUnchanged()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("plain text without expressions", variables);
        Assert.Equal("plain text without expressions", result);
    }

    [Fact]
    public void Evaluate_EmptyInput_ReturnsEmpty()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("", variables);
        Assert.Equal("", result);
    }

    [Fact]
    public void Evaluate_EmptyVariableValue_SubstitutesEmpty()
    {
        var variables = new Dictionary<string, string> { ["name"] = "" };
        var result = TemplateExpressionEvaluator.Evaluate("prefix-{{name}}-suffix", variables);
        Assert.Equal("prefix--suffix", result);
    }

    [Fact]
    public void Evaluate_VariableWithWhitespaceInExpression_TrimsAndResolves()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{ name }}", variables);
        Assert.Equal("MyApp", result);
    }

    [Fact]
    public void Evaluate_EmptyVariables_LeavesExpressionsUnchanged()
    {
        var variables = new Dictionary<string, string>();
        var result = TemplateExpressionEvaluator.Evaluate("{{name}}", variables);
        Assert.Equal("{{name}}", result);
    }

    [Fact]
    public void Evaluate_AdjacentExpressions_SubstitutesAll()
    {
        var variables = new Dictionary<string, string>
        {
            ["a"] = "X",
            ["b"] = "Y"
        };
        var result = TemplateExpressionEvaluator.Evaluate("{{a}}{{b}}", variables);
        Assert.Equal("XY", result);
    }

    #endregion

    #region Filters

    [Theory]
    [InlineData("lowercase", "MyApp", "myapp")]
    [InlineData("uppercase", "MyApp", "MYAPP")]
    [InlineData("kebabcase", "MyApp", "my-app")]
    [InlineData("snakecase", "MyApp", "my_app")]
    [InlineData("camelcase", "MyApp", "myApp")]
    [InlineData("pascalcase", "my-app", "MyApp")]
    public void Evaluate_WithFilter_AppliesTransformation(string filter, string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate($"{{{{name | {filter}}}}}", variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("MyAppName", "my-app-name")]
    [InlineData("myAppName", "my-app-name")]
    [InlineData("HTTPServer", "http-server")]
    [InlineData("SimpleApp", "simple-app")]
    [InlineData("ABC", "abc")]         // all-uppercase → single word
    [InlineData("a", "a")]
    [InlineData("AB", "ab")]            // all-uppercase short word stays together
    public void Evaluate_KebabCase_SplitsWordsCorrectly(string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | kebabcase}}", variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("my-app-name", "my_app_name")]
    [InlineData("MyAppName", "my_app_name")]
    [InlineData("myApp", "my_app")]
    public void Evaluate_SnakeCase_SplitsWordsCorrectly(string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | snakecase}}", variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("my-app-name", "myAppName")]
    [InlineData("MyAppName", "myAppName")]
    [InlineData("MY_APP", "mYApp")]      // word split: M, Y, APP → m + Y + App
    public void Evaluate_CamelCase_SplitsWordsCorrectly(string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | camelcase}}", variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("my-app-name", "MyAppName")]
    [InlineData("myApp", "MyApp")]
    [InlineData("my_app_name", "MyAppName")]
    public void Evaluate_PascalCase_SplitsWordsCorrectly(string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | pascalcase}}", variables);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_FilterWithWhitespace_TrimsAndApplies()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{ name | lowercase }}", variables);
        Assert.Equal("myapp", result);
    }

    [Fact]
    public void Evaluate_UnknownFilter_ReturnsValueUnchanged()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | nonexistentfilter}}", variables);
        Assert.Equal("MyApp", result);
    }

    [Fact]
    public void Evaluate_FilterCaseInsensitive_Works()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | LOWERCASE}}", variables);
        Assert.Equal("myapp", result);
    }

    [Fact]
    public void Evaluate_MixedFilteredAndUnfiltered_Works()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name}} and {{name | lowercase}}", variables);
        Assert.Equal("MyApp and myapp", result);
    }

    [Fact]
    public void Evaluate_FilterOnUnresolvedVariable_LeavesExpressionAsIs()
    {
        var variables = new Dictionary<string, string>();
        var result = TemplateExpressionEvaluator.Evaluate("{{missing | lowercase}}", variables);
        Assert.Equal("{{missing | lowercase}}", result);
    }

    #endregion

    #region Special characters and edge cases

    [Fact]
    public void Evaluate_VariableValueContainsBraces_DoesNotRecurse()
    {
        var variables = new Dictionary<string, string> { ["name"] = "{{other}}" };
        var result = TemplateExpressionEvaluator.Evaluate("{{name}}", variables);
        Assert.Equal("{{other}}", result);
    }

    [Fact]
    public void Evaluate_VariableValueContainsSpecialChars_PreservesValue()
    {
        var variables = new Dictionary<string, string> { ["path"] = "C:\\Users\\test\\file.txt" };
        var result = TemplateExpressionEvaluator.Evaluate("Path: {{path}}", variables);
        Assert.Equal("Path: C:\\Users\\test\\file.txt", result);
    }

    [Fact]
    public void Evaluate_SingleBracePair_NotMatched()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{name}", variables);
        Assert.Equal("{name}", result);
    }

    [Fact]
    public void Evaluate_TripleBraces_RegexMatchesDifferently()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = TemplateExpressionEvaluator.Evaluate("{{{name}}}", variables);
        // The regex {{(.+?)}} matches from pos 1: {{name}} with capture "name"
        // but the greedy first {{ at pos 0 captures "{name" which is unresolved
        // So the expression is left unchanged
        Assert.Equal("{{{name}}}", result);
    }

    [Fact]
    public void Evaluate_MultilineInput_SubstitutesAcrossLines()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var input = "line1 {{name}}\nline2 {{name}}\nline3";
        var result = TemplateExpressionEvaluator.Evaluate(input, variables);
        Assert.Equal("line1 MyApp\nline2 MyApp\nline3", result);
    }

    [Theory]
    [InlineData("word123", "word-123")]
    [InlineData("test42value", "test-42-value")]
    public void Evaluate_KebabCaseWithNumbers_SplitsCorrectly(string inputValue, string expected)
    {
        var variables = new Dictionary<string, string> { ["name"] = inputValue };
        var result = TemplateExpressionEvaluator.Evaluate("{{name | kebabcase}}", variables);
        Assert.Equal(expected, result);
    }

    #endregion
}
