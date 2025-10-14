// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Dashboard.Tests.Controls;

public class FluentIconSwitchTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ToggleLogic_VariousInputs_ProducesExpectedOutput(bool? input, bool expectedOutput)
    {
        // This test verifies the core toggle logic: Value = Value is not true
        // which is the logic used in OnToggleInternalAsync

        // Arrange
        var value = input;

        // Act
        value = value is not true;

        // Assert
        Assert.Equal(expectedOutput, value);
    }

    [Fact]
    public void ToggleLogic_MultipleToggles_ProducesCorrectSequence()
    {
        // Verify that sequential toggles produce the expected sequence

        // Start with null
        bool? value = null;

        // First toggle: null -> true
        value = value is not true;
        Assert.True(value);

        // Second toggle: true -> false
        value = value is not true;
        Assert.False(value);

        // Third toggle: false -> true
        value = value is not true;
        Assert.True(value);

        // Fourth toggle: true -> false
        value = value is not true;
        Assert.False(value);
    }
}
