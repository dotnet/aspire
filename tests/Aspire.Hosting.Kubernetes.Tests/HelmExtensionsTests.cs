// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Kubernetes.Extensions;
using YamlDotNet.Core;

namespace Aspire.Hosting.Kubernetes.Tests;

public class HelmExtensionsTests
{
    [Theory]
    [InlineData("plain string", true, null)]
    [InlineData("{{ .Values.config.myapp.key }}", true, null)]
    [InlineData("{{ .Values.config.myapp.port | int }}", false, ScalarStyle.ForcePlain)]
    [InlineData("{{ .Values.config.myapp.count | int64 }}", false, ScalarStyle.ForcePlain)]
    [InlineData("{{ .Values.config.myapp.rate | float64 }}", false, ScalarStyle.ForcePlain)]
    [InlineData("{{ ternary \"a\" \"b\" (eq .Values.parameters.myapp.flag \"True\") | quote }}", false, ScalarStyle.ForcePlain)]
    [InlineData("{{ ternary \",ssl=true\" \",ssl=false\" (eq .Values.parameters.myapp.enable_tls \"True\") | quote }}", false, ScalarStyle.ForcePlain)]
    [InlineData("{{ if eq .Values.parameters.myapp.enable_tls \"True\" }}{{ .Values.config.myapp.tls_suffix | quote }}{{ else }}{{ \",ssl=false\" | quote }}{{ end }}", false, ScalarStyle.ForcePlain)]
    public void ShouldDoubleQuoteString_ReturnsExpectedResult(string value, bool expectedShouldApply, ScalarStyle? expectedStyle)
    {
        var (shouldApply, style) = HelmExtensions.ShouldDoubleQuoteString(value);

        Assert.Equal(expectedShouldApply, shouldApply);
        Assert.Equal(expectedStyle, style);
    }

    [Theory]
    [InlineData("{{ ternary \"a\" \"b\" true }}")]
    [InlineData("{{ ternary \"val1\" \"val2\" (eq .Values.x \"y\") | quote }}")]
    [InlineData("{{ if eq .Values.parameters.myapp.flag \"True\" }}{{ .Values.config.myapp.suffix | quote }}{{ else }}{{ \"fallback\" | quote }}{{ end }}")]
    public void HelmFlowControlPattern_MatchesFlowControlExpressions(string value)
    {
        Assert.Matches(HelmExtensions.HelmFlowControlPattern(), value);
    }

    [Theory]
    [InlineData("{{ .Values.config.myapp.key }}")]
    [InlineData("plain text")]
    public void HelmFlowControlPattern_DoesNotMatchNonFlowControlExpressions(string value)
    {
        Assert.DoesNotMatch(HelmExtensions.HelmFlowControlPattern(), value);
    }
}
