// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

/// <summary>
/// Tests for the internal logic methods of <see cref="GitTemplate"/>: condition evaluation,
/// variable substitution, CLI value parsing, validation, and kebab-case conversion.
/// These methods are private/static, so we invoke them via reflection for thorough testing.
/// </summary>
public class GitTemplateLogicTests
{
    private static readonly Type s_gitTemplateType = typeof(GitTemplate);

    #region EvaluateCondition

    [Theory]
    [InlineData(null, true)]                               // null → always true
    [InlineData("", true)]                                 // empty → always true
    [InlineData("   ", true)]                              // whitespace → always true
    public void EvaluateCondition_NullOrEmpty_ReturnsTrue(string? condition, bool expected)
    {
        var result = InvokeEvaluateCondition(condition, []);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("useRedis == true", "true", true)]
    [InlineData("useRedis == true", "false", false)]
    [InlineData("useRedis == true", "TRUE", true)]          // case-insensitive
    [InlineData("db == postgres", "postgres", true)]
    [InlineData("db == postgres", "sqlserver", false)]
    public void EvaluateCondition_Equality_Works(string condition, string value, bool expected)
    {
        var variables = new Dictionary<string, string> { ["useRedis"] = value, ["db"] = value };
        var result = InvokeEvaluateCondition(condition, variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("useRedis != true", "true", false)]
    [InlineData("useRedis != true", "false", true)]
    [InlineData("db != postgres", "sqlserver", true)]
    [InlineData("db != postgres", "postgres", false)]
    public void EvaluateCondition_Inequality_Works(string condition, string value, bool expected)
    {
        var variables = new Dictionary<string, string> { ["useRedis"] = value, ["db"] = value };
        var result = InvokeEvaluateCondition(condition, variables);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("myFlag", "true", true)]
    [InlineData("myFlag", "yes", true)]
    [InlineData("myFlag", "anything", true)]
    [InlineData("myFlag", "false", false)]
    [InlineData("myFlag", "", false)]
    public void EvaluateCondition_BareVariable_TruthyCheck(string condition, string value, bool expected)
    {
        var variables = new Dictionary<string, string> { ["myFlag"] = value };
        var result = InvokeEvaluateCondition(condition, variables);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvaluateCondition_UndefinedVariable_ReturnsFalse()
    {
        var result = InvokeEvaluateCondition("missingVar", new Dictionary<string, string>());
        Assert.False(result);
    }

    [Fact]
    public void EvaluateCondition_UndefinedVariable_EqualityCheck_ReturnsFalse()
    {
        var result = InvokeEvaluateCondition("missing == value", new Dictionary<string, string>());
        Assert.False(result);
    }

    [Fact]
    public void EvaluateCondition_UndefinedVariable_EqualityToEmpty_ReturnsTrue()
    {
        // missing variable resolves to "" which equals ""
        var result = InvokeEvaluateCondition("missing == ", new Dictionary<string, string>());
        Assert.True(result);
    }

    [Fact]
    public void EvaluateCondition_SpacesAroundOperator_Trimmed()
    {
        var variables = new Dictionary<string, string> { ["x"] = "y" };
        var result = InvokeEvaluateCondition("  x  ==  y  ", variables);
        Assert.True(result);
    }

    #endregion

    #region SubstituteVariables

    [Fact]
    public void SubstituteVariables_ReplacesPlaceholders()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp", ["port"] = "5000" };
        var result = InvokeSubstituteVariables("cd {{name}} && run on {{port}}", variables);
        Assert.Equal("cd MyApp && run on 5000", result);
    }

    [Fact]
    public void SubstituteVariables_NoPlaceholders_Unchanged()
    {
        var variables = new Dictionary<string, string> { ["name"] = "MyApp" };
        var result = InvokeSubstituteVariables("no placeholders here", variables);
        Assert.Equal("no placeholders here", result);
    }

    [Fact]
    public void SubstituteVariables_MissingVariable_LeftAsIs()
    {
        var variables = new Dictionary<string, string>();
        var result = InvokeSubstituteVariables("{{missing}}", variables);
        Assert.Equal("{{missing}}", result);
    }

    [Fact]
    public void SubstituteVariables_CaseInsensitive()
    {
        var variables = new Dictionary<string, string> { ["ProjectName"] = "MyApp" };
        var result = InvokeSubstituteVariables("{{projectname}}", variables);
        Assert.Equal("MyApp", result);
    }

    #endregion

    #region ToKebabCase

    [Theory]
    [InlineData("useRedis", "use-redis")]
    [InlineData("useRedisCache", "use-redis-cache")]
    [InlineData("ABC", "a-b-c")]
    [InlineData("myApp", "my-app")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData("Simple", "simple")]
    [InlineData("alllowercase", "alllowercase")]
    [InlineData("ALLUPPERCASE", "a-l-l-u-p-p-e-r-c-a-s-e")]
    public void ToKebabCase_ConvertsCorrectly(string input, string expected)
    {
        var result = InvokeToKebabCase(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region ValidateCliValue

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("True")]
    [InlineData("False")]
    public void ValidateCliValue_Boolean_ValidValues_ReturnsNull(string value)
    {
        var varDef = new GitTemplateVariable { Type = "boolean" };
        var result = InvokeValidateCliValue("flag", value, varDef);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("on")]
    [InlineData("notabool")]
    public void ValidateCliValue_Boolean_InvalidValues_ReturnsError(string value)
    {
        var varDef = new GitTemplateVariable { Type = "boolean" };
        var result = InvokeValidateCliValue("flag", value, varDef);
        Assert.NotNull(result);
        Assert.Contains("true", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("false", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCliValue_Choice_ValidValue_ReturnsNull()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "choice",
            Choices =
            [
                new GitTemplateVariableChoice { Value = "postgres" },
                new GitTemplateVariableChoice { Value = "sqlserver" }
            ]
        };
        var result = InvokeValidateCliValue("db", "postgres", varDef);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateCliValue_Choice_CaseInsensitive_ReturnsNull()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "choice",
            Choices = [new GitTemplateVariableChoice { Value = "postgres" }]
        };
        var result = InvokeValidateCliValue("db", "POSTGRES", varDef);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateCliValue_Choice_InvalidValue_ReturnsError()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "choice",
            Choices = [new GitTemplateVariableChoice { Value = "postgres" }]
        };
        var result = InvokeValidateCliValue("db", "mysql", varDef);
        Assert.NotNull(result);
        Assert.Contains("postgres", result);
    }

    [Theory]
    [InlineData("42", null, null, null)]        // valid int, no bounds
    [InlineData("0", null, null, null)]          // zero
    [InlineData("-5", null, null, null)]          // negative
    [InlineData("1024", 1024, 65535, null)]       // at min
    [InlineData("65535", 1024, 65535, null)]      // at max
    [InlineData("5000", 1024, 65535, null)]       // in range
    public void ValidateCliValue_Integer_ValidValues_ReturnsNull(
        string value, int? min, int? max, string? expectedError)
    {
        var varDef = new GitTemplateVariable
        {
            Type = "integer",
            Validation = (min.HasValue || max.HasValue) ? new GitTemplateVariableValidation { Min = min, Max = max } : null
        };
        var result = InvokeValidateCliValue("port", value, varDef);
        Assert.Equal(expectedError, result);
    }

    [Fact]
    public void ValidateCliValue_Integer_BelowMin_ReturnsError()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "integer",
            Validation = new GitTemplateVariableValidation { Min = 1024 }
        };
        var result = InvokeValidateCliValue("port", "100", varDef);
        Assert.NotNull(result);
        Assert.Contains("1024", result);
    }

    [Fact]
    public void ValidateCliValue_Integer_AboveMax_ReturnsError()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "integer",
            Validation = new GitTemplateVariableValidation { Max = 65535 }
        };
        var result = InvokeValidateCliValue("port", "70000", varDef);
        Assert.NotNull(result);
        Assert.Contains("65535", result);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("3.14")]
    [InlineData("abc")]
    public void ValidateCliValue_Integer_NonInteger_ReturnsError(string value)
    {
        var varDef = new GitTemplateVariable { Type = "integer" };
        var result = InvokeValidateCliValue("count", value, varDef);
        Assert.NotNull(result);
        Assert.Contains("integer", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCliValue_String_MatchesPattern_ReturnsNull()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "string",
            Validation = new GitTemplateVariableValidation { Pattern = "^[a-z]+$" }
        };
        var result = InvokeValidateCliValue("name", "myapp", varDef);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateCliValue_String_FailsPattern_ReturnsError()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "string",
            Validation = new GitTemplateVariableValidation
            {
                Pattern = "^[a-z]+$",
                Message = "Must be lowercase letters only"
            }
        };
        var result = InvokeValidateCliValue("name", "MyApp123", varDef);
        Assert.NotNull(result);
        Assert.Equal("Must be lowercase letters only", result);
    }

    [Fact]
    public void ValidateCliValue_String_FailsPattern_DefaultMessage()
    {
        var varDef = new GitTemplateVariable
        {
            Type = "string",
            Validation = new GitTemplateVariableValidation { Pattern = "^[a-z]+$" }
        };
        var result = InvokeValidateCliValue("name", "UPPER", varDef);
        Assert.NotNull(result);
        Assert.Contains("pattern", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCliValue_String_NoValidation_ReturnsNull()
    {
        var varDef = new GitTemplateVariable { Type = "string" };
        var result = InvokeValidateCliValue("name", "anything goes", varDef);
        Assert.Null(result);
    }

    #endregion

    #region ParseUnmatchedTokens

    [Fact]
    public void ParseUnmatchedTokens_KeyValuePairs_Parsed()
    {
        var tokens = new[] { "--name", "MyApp", "--port", "5000" };
        var result = InvokeParseUnmatchedTokens(tokens);
        Assert.Equal("MyApp", result["name"]);
        Assert.Equal("5000", result["port"]);
    }

    [Fact]
    public void ParseUnmatchedTokens_BareFlag_TreatedAsBoolTrue()
    {
        var tokens = new[] { "--useRedis" };
        var result = InvokeParseUnmatchedTokens(tokens);
        Assert.Equal("true", result["useRedis"]);
    }

    [Fact]
    public void ParseUnmatchedTokens_MixedFlagsAndValues_Parsed()
    {
        var tokens = new[] { "--useRedis", "--name", "MyApp", "--verbose" };
        var result = InvokeParseUnmatchedTokens(tokens);
        Assert.Equal("true", result["useRedis"]);
        Assert.Equal("MyApp", result["name"]);
        Assert.Equal("true", result["verbose"]);
    }

    [Fact]
    public void ParseUnmatchedTokens_NonDashToken_Ignored()
    {
        var tokens = new[] { "positional", "--name", "MyApp" };
        var result = InvokeParseUnmatchedTokens(tokens);
        Assert.Single(result);
        Assert.Equal("MyApp", result["name"]);
    }

    [Fact]
    public void ParseUnmatchedTokens_EmptyTokens_ReturnsEmpty()
    {
        var result = InvokeParseUnmatchedTokens([]);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseUnmatchedTokens_ConsecutiveFlags_AllTreatedAsTrue()
    {
        var tokens = new[] { "--a", "--b", "--c" };
        var result = InvokeParseUnmatchedTokens(tokens);
        Assert.Equal("true", result["a"]);
        Assert.Equal("true", result["b"]);
        Assert.Equal("true", result["c"]);
    }

    #endregion

    #region TryGetCliValue

    [Fact]
    public void TryGetCliValue_ExactMatch_ReturnsValue()
    {
        var cliValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["useRedis"] = "true"
        };
        var found = InvokeTryGetCliValue(cliValues, "useRedis", out var value);
        Assert.True(found);
        Assert.Equal("true", value);
    }

    [Fact]
    public void TryGetCliValue_KebabCaseMatch_ReturnsValue()
    {
        var cliValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["use-redis"] = "true"
        };
        var found = InvokeTryGetCliValue(cliValues, "useRedis", out var value);
        Assert.True(found);
        Assert.Equal("true", value);
    }

    [Fact]
    public void TryGetCliValue_NoMatch_ReturnsFalse()
    {
        var cliValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var found = InvokeTryGetCliValue(cliValues, "useRedis", out var value);
        Assert.False(found);
        Assert.Equal("", value);
    }

    #endregion

    #region Reflection helpers

    private static bool InvokeEvaluateCondition(string? condition, Dictionary<string, string> variables)
    {
        var method = s_gitTemplateType.GetMethod("EvaluateCondition", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (bool)method.Invoke(null, [condition, variables])!;
    }

    private static string InvokeSubstituteVariables(string text, Dictionary<string, string> variables)
    {
        var method = s_gitTemplateType.GetMethod("SubstituteVariables", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, [text, variables])!;
    }

    private static string InvokeToKebabCase(string input)
    {
        var method = s_gitTemplateType.GetMethod("ToKebabCase", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, [input])!;
    }

    private static string? InvokeValidateCliValue(string varName, string value, GitTemplateVariable varDef)
    {
        var method = s_gitTemplateType.GetMethod("ValidateCliValue", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string?)method.Invoke(null, [varName, value, varDef]);
    }

    private static Dictionary<string, string> InvokeParseUnmatchedTokens(string[] tokens)
    {
        // Create a mock ParseResult — we need to simulate unmatched tokens
        // ParseUnmatchedTokens works with parseResult.UnmatchedTokens which is an IReadOnlyList<string>
        // Since ParseResult is complex, we'll test via the raw logic
        var method = s_gitTemplateType.GetMethod("ParseUnmatchedTokens", BindingFlags.Static | BindingFlags.NonPublic)!;

        // We need a real ParseResult. Let's create one with unmatched tokens.
        var rootCommand = new System.CommandLine.RootCommand { TreatUnmatchedTokensAsErrors = false };
        var parseResult = rootCommand.Parse(tokens);
        return (Dictionary<string, string>)method.Invoke(null, [parseResult])!;
    }

    private static bool InvokeTryGetCliValue(Dictionary<string, string> cliValues, string varName, out string value)
    {
        var method = s_gitTemplateType.GetMethod("TryGetCliValue", BindingFlags.Static | BindingFlags.NonPublic)!;
        var parameters = new object?[] { cliValues, varName, null };
        var result = (bool)method.Invoke(null, parameters)!;
        value = (string)parameters[2]!;
        return result;
    }

    #endregion
}
