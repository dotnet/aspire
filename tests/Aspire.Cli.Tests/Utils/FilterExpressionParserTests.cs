// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class FilterExpressionParserTests
{
    [Fact]
    public void Parse_EqualsOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("http.method=POST");

        Assert.Equal("http.method", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("POST", result.Value);
    }

    [Fact]
    public void Parse_NotEqualsOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("status!=Error");

        Assert.Equal("status", result.Field);
        Assert.Equal(FilterCondition.NotEqual, result.Condition);
        Assert.Equal("Error", result.Value);
    }

    [Fact]
    public void Parse_ContainsOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("user.id~admin");

        Assert.Equal("user.id", result.Field);
        Assert.Equal(FilterCondition.Contains, result.Condition);
        Assert.Equal("admin", result.Value);
    }

    [Fact]
    public void Parse_NotContainsOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("msg!~timeout");

        Assert.Equal("msg", result.Field);
        Assert.Equal(FilterCondition.NotContains, result.Condition);
        Assert.Equal("timeout", result.Value);
    }

    [Fact]
    public void Parse_GreaterThanOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("status_code>399");

        Assert.Equal("status_code", result.Field);
        Assert.Equal(FilterCondition.GreaterThan, result.Condition);
        Assert.Equal("399", result.Value);
    }

    [Fact]
    public void Parse_LessThanOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("duration<100");

        Assert.Equal("duration", result.Field);
        Assert.Equal(FilterCondition.LessThan, result.Condition);
        Assert.Equal("100", result.Value);
    }

    [Fact]
    public void Parse_GreaterThanOrEqualOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("level>=Warning");

        Assert.Equal("level", result.Field);
        Assert.Equal(FilterCondition.GreaterThanOrEqual, result.Condition);
        Assert.Equal("Warning", result.Value);
    }

    [Fact]
    public void Parse_LessThanOrEqualOperator_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("level<=Info");

        Assert.Equal("level", result.Field);
        Assert.Equal(FilterCondition.LessThanOrEqual, result.Condition);
        Assert.Equal("Info", result.Value);
    }

    [Fact]
    public void Parse_FieldWithDots_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("http.response.status_code=200");

        Assert.Equal("http.response.status_code", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("200", result.Value);
    }

    [Fact]
    public void Parse_ValueWithSpaces_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("message~connection refused");

        Assert.Equal("message", result.Field);
        Assert.Equal(FilterCondition.Contains, result.Condition);
        Assert.Equal("connection refused", result.Value);
    }

    [Fact]
    public void Parse_EmptyExpression_ThrowsException()
    {
        var exception = Assert.Throws<FilterParseException>(() => FilterExpressionParser.Parse(""));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Parse_WhitespaceExpression_ThrowsException()
    {
        var exception = Assert.Throws<FilterParseException>(() => FilterExpressionParser.Parse("   "));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Parse_MissingOperator_ThrowsException()
    {
        var exception = Assert.Throws<FilterParseException>(() => FilterExpressionParser.Parse("fieldvalue"));

        Assert.Contains("Invalid filter expression", exception.Message);
    }

    [Fact]
    public void Parse_MissingValue_ReturnsEmptyValue()
    {
        var result = FilterExpressionParser.Parse("field=");

        Assert.Equal("field", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("", result.Value);
    }

    [Fact]
    public void Parse_MissingField_ThrowsException()
    {
        var exception = Assert.Throws<FilterParseException>(() => FilterExpressionParser.Parse("=value"));

        Assert.Contains("Field name is missing", exception.Message);
    }

    [Fact]
    public void Parse_FieldWithUnderscores_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("my_custom_field=test");

        Assert.Equal("my_custom_field", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Parse_ValueWithEqualsSign_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("query=key=value");

        Assert.Equal("query", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("key=value", result.Value);
    }

    [Fact]
    public void Parse_NumericValue_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("count>=10");

        Assert.Equal("count", result.Field);
        Assert.Equal(FilterCondition.GreaterThanOrEqual, result.Condition);
        Assert.Equal("10", result.Value);
    }

    [Fact]
    public void Parse_DoubleValue_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("duration<1.5");

        Assert.Equal("duration", result.Field);
        Assert.Equal(FilterCondition.LessThan, result.Condition);
        Assert.Equal("1.5", result.Value);
    }

    [Fact]
    public void Parse_NegativeValue_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("offset>-100");

        Assert.Equal("offset", result.Field);
        Assert.Equal(FilterCondition.GreaterThan, result.Condition);
        Assert.Equal("-100", result.Value);
    }

    [Theory]
    [InlineData("field=value", "field", FilterCondition.Equals, "value")]
    [InlineData("field!=value", "field", FilterCondition.NotEqual, "value")]
    [InlineData("field~value", "field", FilterCondition.Contains, "value")]
    [InlineData("field!~value", "field", FilterCondition.NotContains, "value")]
    [InlineData("field>value", "field", FilterCondition.GreaterThan, "value")]
    [InlineData("field<value", "field", FilterCondition.LessThan, "value")]
    [InlineData("field>=value", "field", FilterCondition.GreaterThanOrEqual, "value")]
    [InlineData("field<=value", "field", FilterCondition.LessThanOrEqual, "value")]
    public void Parse_AllOperators_ReturnsCorrectCondition(string expression, string expectedField, FilterCondition expectedCondition, string expectedValue)
    {
        var result = FilterExpressionParser.Parse(expression);

        Assert.Equal(expectedField, result.Field);
        Assert.Equal(expectedCondition, result.Condition);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void TryParse_ValidExpression_ReturnsTrueAndFilter()
    {
        var success = FilterExpressionParser.TryParse("http.method=GET", out var filter);

        Assert.True(success);
        Assert.NotNull(filter);
        Assert.Equal("http.method", filter.Field);
        Assert.Equal(FilterCondition.Equals, filter.Condition);
        Assert.Equal("GET", filter.Value);
    }

    [Fact]
    public void TryParse_InvalidExpression_ReturnsFalseAndNull()
    {
        var success = FilterExpressionParser.TryParse("invalid", out var filter);

        Assert.False(success);
        Assert.Null(filter);
    }

    [Fact]
    public void TryParse_EmptyExpression_ReturnsFalseAndNull()
    {
        var success = FilterExpressionParser.TryParse("", out var filter);

        Assert.False(success);
        Assert.Null(filter);
    }

    [Theory]
    [InlineData(FilterCondition.Equals, "=")]
    [InlineData(FilterCondition.NotEqual, "!=")]
    [InlineData(FilterCondition.Contains, "~")]
    [InlineData(FilterCondition.NotContains, "!~")]
    [InlineData(FilterCondition.GreaterThan, ">")]
    [InlineData(FilterCondition.LessThan, "<")]
    [InlineData(FilterCondition.GreaterThanOrEqual, ">=")]
    [InlineData(FilterCondition.LessThanOrEqual, "<=")]
    public void ConditionToOperator_AllConditions_ReturnsCorrectOperator(FilterCondition condition, string expectedOperator)
    {
        var result = FilterExpressionParser.ConditionToOperator(condition);

        Assert.Equal(expectedOperator, result);
    }

    [Theory]
    [InlineData(FilterCondition.Equals, "equals")]
    [InlineData(FilterCondition.NotEqual, "not equals")]
    [InlineData(FilterCondition.Contains, "contains")]
    [InlineData(FilterCondition.NotContains, "not contains")]
    [InlineData(FilterCondition.GreaterThan, "greater than")]
    [InlineData(FilterCondition.LessThan, "less than")]
    [InlineData(FilterCondition.GreaterThanOrEqual, "greater than or equal")]
    [InlineData(FilterCondition.LessThanOrEqual, "less than or equal")]
    public void ConditionToDescription_AllConditions_ReturnsCorrectDescription(FilterCondition condition, string expectedDescription)
    {
        var result = FilterExpressionParser.ConditionToDescription(condition);

        Assert.Equal(expectedDescription, result);
    }

    [Fact]
    public void Parse_FieldWithLeadingWhitespace_TrimsField()
    {
        var result = FilterExpressionParser.Parse("  field=value");

        Assert.Equal("field", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Parse_FieldWithTrailingWhitespace_TrimsField()
    {
        var result = FilterExpressionParser.Parse("field  =value");

        Assert.Equal("field", result.Field);
        Assert.Equal(FilterCondition.Equals, result.Condition);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Parse_ValueWithSpecialCharacters_ReturnsCorrectFilter()
    {
        var result = FilterExpressionParser.Parse("path~/api/users?id=123&name=test");

        Assert.Equal("path", result.Field);
        Assert.Equal(FilterCondition.Contains, result.Condition);
        Assert.Equal("/api/users?id=123&name=test", result.Value);
    }

    [Fact]
    public void Parse_GreaterThanFollowedByEquals_ParsesAsGreaterThanOrEqual()
    {
        var result = FilterExpressionParser.Parse("value>=10");

        Assert.Equal("value", result.Field);
        Assert.Equal(FilterCondition.GreaterThanOrEqual, result.Condition);
        Assert.Equal("10", result.Value);
    }

    [Fact]
    public void Parse_LessThanFollowedByEquals_ParsesAsLessThanOrEqual()
    {
        var result = FilterExpressionParser.Parse("value<=10");

        Assert.Equal("value", result.Field);
        Assert.Equal(FilterCondition.LessThanOrEqual, result.Condition);
        Assert.Equal("10", result.Value);
    }

    [Fact]
    public void Parse_NotFollowedByEquals_ParsesAsNotEqual()
    {
        var result = FilterExpressionParser.Parse("status!=failed");

        Assert.Equal("status", result.Field);
        Assert.Equal(FilterCondition.NotEqual, result.Condition);
        Assert.Equal("failed", result.Value);
    }

    [Fact]
    public void Parse_NotFollowedByTilde_ParsesAsNotContains()
    {
        var result = FilterExpressionParser.Parse("message!~error");

        Assert.Equal("message", result.Field);
        Assert.Equal(FilterCondition.NotContains, result.Condition);
        Assert.Equal("error", result.Value);
    }

    [Fact]
    public void ToTelemetryFilter_EqualsCondition_ReturnsFieldTelemetryFilter()
    {
        var filter = FilterExpressionParser.Parse("http.method=POST");

        var telemetryFilter = filter.ToTelemetryFilter();

        Assert.Equal("http.method", telemetryFilter.Field);
        Assert.Equal("equals", telemetryFilter.Condition);
        Assert.Equal("POST", telemetryFilter.Value);
        Assert.True(telemetryFilter.Enabled);
    }

    [Theory]
    [InlineData(FilterCondition.Equals, "equals")]
    [InlineData(FilterCondition.NotEqual, "!equals")]
    [InlineData(FilterCondition.Contains, "contains")]
    [InlineData(FilterCondition.NotContains, "!contains")]
    [InlineData(FilterCondition.GreaterThan, "gt")]
    [InlineData(FilterCondition.LessThan, "lt")]
    [InlineData(FilterCondition.GreaterThanOrEqual, "gte")]
    [InlineData(FilterCondition.LessThanOrEqual, "lte")]
    public void ToTelemetryFilter_AllConditions_MapsCorrectly(FilterCondition condition, string expectedConditionString)
    {
        var filter = new ParsedFilter("field", condition, "value");

        var telemetryFilter = filter.ToTelemetryFilter();

        Assert.Equal("field", telemetryFilter.Field);
        Assert.Equal(expectedConditionString, telemetryFilter.Condition);
        Assert.Equal("value", telemetryFilter.Value);
    }

    [Fact]
    public void ToTelemetryFilter_PreservesFieldNameWithDots()
    {
        var filter = FilterExpressionParser.Parse("http.response.status_code=200");

        var telemetryFilter = filter.ToTelemetryFilter();

        Assert.Equal("http.response.status_code", telemetryFilter.Field);
        Assert.Equal("equals", telemetryFilter.Condition);
        Assert.Equal("200", telemetryFilter.Value);
    }

    [Fact]
    public void ToTelemetryFilter_PreservesValueWithSpaces()
    {
        var filter = FilterExpressionParser.Parse("message~connection refused");

        var telemetryFilter = filter.ToTelemetryFilter();

        Assert.Equal("message", telemetryFilter.Field);
        Assert.Equal("contains", telemetryFilter.Condition);
        Assert.Equal("connection refused", telemetryFilter.Value);
    }

    [Fact]
    public void ToTelemetryFilter_EnabledByDefault()
    {
        var filter = FilterExpressionParser.Parse("status=Error");

        var telemetryFilter = filter.ToTelemetryFilter();

        Assert.True(telemetryFilter.Enabled);
    }

    [Theory]
    [InlineData(FilterCondition.Equals, "equals")]
    [InlineData(FilterCondition.NotEqual, "!equals")]
    [InlineData(FilterCondition.Contains, "contains")]
    [InlineData(FilterCondition.NotContains, "!contains")]
    [InlineData(FilterCondition.GreaterThan, "gt")]
    [InlineData(FilterCondition.LessThan, "lt")]
    [InlineData(FilterCondition.GreaterThanOrEqual, "gte")]
    [InlineData(FilterCondition.LessThanOrEqual, "lte")]
    public void ToTelemetryConditionString_AllConditions_ReturnsCorrectString(FilterCondition condition, string expectedString)
    {
        var result = condition.ToTelemetryConditionString();

        Assert.Equal(expectedString, result);
    }
}
