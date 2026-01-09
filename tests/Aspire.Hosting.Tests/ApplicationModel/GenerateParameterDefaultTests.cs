// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.ApplicationModel;

public class GenerateParameterDefaultTests
{
    [Fact]
    public void MinLength_Throws_WhenLessThanOrEqualZero()
    {
        var gd = new GenerateParameterDefault() { MinLength = 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => gd.MinLength = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => gd.MinLength = -1);
    }

    [Fact]
    public void GetDefaultValue_Respects_Length_And_LowerOnly()
    {
        var gd = new GenerateParameterDefault
        {
            MinLength = 10,
            Lower = true,
            Upper = false,
            Numeric = false,
            Special = false,
            MinLower = 0,
            MinUpper = 0,
            MinNumeric = 0,
            MinSpecial = 0
        };

        var value = gd.GetDefaultValue();

        Assert.Equal(10, value.Length);
        Assert.True(value.All(PasswordGenerator.LowerCaseChars.Contains));
    }

    [Fact]
    public void GetDefaultValue_Respects_Minimum_Type_Counts()
    {
        var gd = new GenerateParameterDefault
        {
            MinLength = 12,
            Lower = true,
            Upper = true,
            Numeric = true,
            Special = true,
            MinLower = 2,
            MinUpper = 1,
            MinNumeric = 1,
            MinSpecial = 2
        };

        var value = gd.GetDefaultValue();

        Assert.True(value.Count(PasswordGenerator.LowerCaseChars.Contains) >= gd.MinLower);
        Assert.True(value.Count(PasswordGenerator.UpperCaseChars.Contains) >= gd.MinUpper);
        Assert.True(value.Count(PasswordGenerator.NumericChars.Contains) >= gd.MinNumeric);
        Assert.True(value.Count(PasswordGenerator.SpecialChars.Contains) >= gd.MinSpecial);
        Assert.True(value.Length >= 12);
    }
}
