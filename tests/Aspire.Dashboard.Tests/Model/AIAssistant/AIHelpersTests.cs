// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant;
using Xunit;

namespace Aspire.Dashboard.Tests.Model.AIAssistant;

public class AIHelpersTests
{
    [Fact]
    public void LimitLength_UnderLimit_ReturnFullValue()
    {
        var value = AIHelpers.LimitLength("How now brown cow?");
        Assert.Equal("How now brown cow?", value);
    }

    [Fact]
    public void LimitLength_OverLimit_ReturnTrimmedValue()
    {
        var value = AIHelpers.LimitLength(new string('!', 10_000));
        Assert.Equal($"{new string('!', AIHelpers.MaximumStringLength)}...[TRUNCATED]", value);
    }
}
