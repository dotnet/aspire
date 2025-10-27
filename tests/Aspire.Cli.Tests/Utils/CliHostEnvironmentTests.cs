// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Utils;

public class CliHostEnvironmentTests
{
    [Fact]
    public void SupportsInteractiveInput_ReturnsTrue_WhenNoConfigSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsInteractiveOutput_ReturnsTrue_WhenNoConfigSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveOutput);
    }

    [Fact]
    public void SupportsAnsi_ReturnsTrue_WhenNoConfigSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }

    [Theory]
    [InlineData("ASPIRE_NON_INTERACTIVE", "true")]
    [InlineData("ASPIRE_NON_INTERACTIVE", "1")]
    public void SupportsInteractiveInput_ReturnsFalse_WhenNonInteractiveSet(string key, string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = value
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsInteractiveInput);
    }

    [Theory]
    [InlineData("ASPIRE_NON_INTERACTIVE", "true")]
    [InlineData("ASPIRE_NON_INTERACTIVE", "1")]
    public void SupportsInteractiveOutput_ReturnsFalse_WhenNonInteractiveSet(string key, string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = value
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsInteractiveOutput);
    }

    [Theory]
    [InlineData("CI", "true")]
    [InlineData("CI", "1")]
    [InlineData("GITHUB_ACTIONS", "true")]
    [InlineData("AZURE_PIPELINES", "True")]
    [InlineData("TF_BUILD", "1")]
    public void SupportsInteractiveInput_ReturnsFalse_InCIEnvironment(string envVar, string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [envVar] = value
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsInteractiveInput);
    }

    [Theory]
    [InlineData("CI", "true")]
    [InlineData("CI", "1")]
    [InlineData("GITHUB_ACTIONS", "true")]
    [InlineData("AZURE_PIPELINES", "True")]
    public void SupportsInteractiveOutput_ReturnsFalse_InCIEnvironment(string envVar, string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [envVar] = value
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsInteractiveOutput);
    }

    [Theory]
    [InlineData("CI", "true")]
    [InlineData("CI", "1")]
    [InlineData("GITHUB_ACTIONS", "true")]
    public void SupportsAnsi_ReturnsTrue_InCIEnvironment(string envVar, string value)
    {
        // Arrange - ANSI should still be supported in CI for colored output
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [envVar] = value
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }

    [Fact]
    public void SupportsAnsi_ReturnsFalse_WhenNO_COLORSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NO_COLOR"] = "1"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsAnsi);
    }

    [Fact]
    public void SupportsInteractiveInput_ReturnsFalse_WhenNonInteractiveTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        Assert.False(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsInteractiveOutput_ReturnsFalse_WhenNonInteractiveTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        Assert.False(env.SupportsInteractiveOutput);
    }

    [Fact]
    public void SupportsAnsi_ReturnsTrue_WhenNonInteractiveTrue()
    {
        // Arrange - ANSI should still be supported even in non-interactive mode
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }

    [Fact]
    public void SupportsInteractiveInput_ReturnsTrue_WhenPlaygroundModeSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsInteractiveOutput_ReturnsTrue_WhenPlaygroundModeSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveOutput);
    }

    [Fact]
    public void SupportsInteractiveInput_ReturnsTrue_WhenPlaygroundModeSet_EvenInCI()
    {
        // Arrange - ASPIRE_PLAYGROUND should override CI environment detection
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true",
                ["CI"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsInteractiveOutput_ReturnsTrue_WhenPlaygroundModeSet_EvenInCI()
    {
        // Arrange - ASPIRE_PLAYGROUND should override CI environment detection
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true",
                ["GITHUB_ACTIONS"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsInteractiveOutput);
    }

    [Fact]
    public void SupportsInteractiveInput_ReturnsTrue_WhenPlaygroundModeSet_ButNonInteractiveIsTrue()
    {
        // Arrange - ASPIRE_PLAYGROUND should take precedence over --non-interactive flag
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        // ASPIRE_PLAYGROUND takes precedence over the --non-interactive flag
        Assert.True(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsInteractiveOutput_ReturnsTrue_WhenPlaygroundModeSet_ButNonInteractiveIsTrue()
    {
        // Arrange - ASPIRE_PLAYGROUND should take precedence over --non-interactive flag
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        // ASPIRE_PLAYGROUND takes precedence over the --non-interactive flag
        Assert.True(env.SupportsInteractiveOutput);
    }

    [Fact]
    public void SupportsInteractiveInput_ReturnsFalse_WhenPlaygroundModeSetToFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "false",
                ["CI"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.False(env.SupportsInteractiveInput);
    }

    [Fact]
    public void SupportsAnsi_ReturnsTrue_WhenPlaygroundModeSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }

    [Fact]
    public void SupportsAnsi_ReturnsTrue_WhenPlaygroundModeSet_EvenWithNO_COLOR()
    {
        // Arrange - ASPIRE_PLAYGROUND should override NO_COLOR
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true",
                ["NO_COLOR"] = "1"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: false);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }

    [Fact]
    public void SupportsAnsi_ReturnsTrue_WhenPlaygroundModeSet_WithNonInteractive()
    {
        // Arrange - ASPIRE_PLAYGROUND should enable ANSI even with --non-interactive
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_PLAYGROUND"] = "true"
            })
            .Build();
        
        // Act
        var env = new CliHostEnvironment(configuration, nonInteractive: true);
        
        // Assert
        Assert.True(env.SupportsAnsi);
    }
}
