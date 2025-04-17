// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class PublishCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("publish --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PublishCommandFailsWithInvalidProjectFile()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetAppHostInformationAsyncCallback = (projectFile, cancellationToken) =>
                {
                    return (1, false, null); // Simulate failure to retrieve app host information
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project invalid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }
}
