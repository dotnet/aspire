// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Aspire.Hosting.Analyzers.Tests;

public class AnalyzersPublicApiTests
{
    [Fact]
    public void InitializeShouldThrowWhenContextIsNull()
    {
        var appHostAnalyzer = new AppHostAnalyzer();
        AnalysisContext context = null!;

        var action = () => appHostAnalyzer.Initialize(context);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(context), exception.ParamName);
    }
}
