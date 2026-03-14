// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands.Sdk;
using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class SdkDumpCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task SdkDumpWithHelpReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump --help");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("Json")]
    [InlineData("JSON")]
    [InlineData("ci")]
    [InlineData("Ci")]
    [InlineData("pretty")]
    [InlineData("Pretty")]
    public void ParsesFormatOptionWithoutErrors(string format)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"sdk dump --format {format}");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SdkDumpWithNonexistentCsprojReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump /nonexistent/path/to/integration.csproj");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithEmptyPackageNameReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump @13.2.0");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithEmptyVersionReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithInvalidVersionFormatReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@not-a-version!!!");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task SdkDumpWithInvalidArgumentFormatReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump some-random-string");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public void ParsesValidPackageFormatWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ParsesMultipleMixedArgumentsWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0 Aspire.Hosting.PostgreSQL@13.2.0");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ParsesPreReleaseVersionWithoutErrors()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@13.2.0-preview.1");

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task SdkDumpWithDoubleAtSignReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        // LastIndexOf('@') splits as "Aspire.Hosting.Redis@" and "13.2.0"
        // The @ in the package name part triggers format validation
        var result = command.Parse("sdk dump Aspire.Hosting.Redis@@13.2.0");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // "Aspire.Hosting.Redis@" is not a valid semver, so it should fail version validation
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public void FormatPretty_UsesMethodFamilyNameWhenPresent()
    {
        var capabilities = new CapabilitiesInfo
        {
            Capabilities =
            [
                new CapabilityInfo
                {
                    CapabilityId = "Aspire.Hosting/addConnectionStringExpression",
                    MethodName = "addConnectionStringExpression",
                    MethodFamilyName = "addConnectionString",
                    OwningTypeName = "DistributedApplicationBuilder",
                    ReturnType = new TypeRefInfo { TypeId = "Aspire.Hosting/Aspire.Hosting.ConnectionStringResource" },
                    Parameters =
                    [
                        new ParameterInfo { Name = "name", Type = new TypeRefInfo { TypeId = "string" } },
                        new ParameterInfo { Name = "connectionStringExpression", Type = new TypeRefInfo { TypeId = "Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression" } }
                    ]
                }
            ]
        };

        var output = InvokePrivateFormatter("FormatPretty", capabilities);

        Assert.Contains("addConnectionString(name: string, connectionStringExpression: ReferenceExpression)", output);
        Assert.DoesNotContain("addConnectionStringExpression(", output);
    }

    [Fact]
    public void FormatJson_IncludesMethodFamilyName()
    {
        var capabilities = new CapabilitiesInfo
        {
            Capabilities =
            [
                new CapabilityInfo
                {
                    CapabilityId = "Aspire.Hosting/addExternalServiceUri",
                    MethodName = "addExternalServiceUri",
                    MethodFamilyName = "addExternalService"
                }
            ]
        };

        var output = InvokePrivateFormatter("FormatJson", capabilities);

        Assert.Contains("\"MethodFamilyName\"", output);
        Assert.Contains("addExternalService", output);
    }

    private static string InvokePrivateFormatter(string methodName, CapabilitiesInfo capabilities)
    {
        var method = typeof(SdkDumpCommand).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        return Assert.IsType<string>(method.Invoke(null, [capabilities]));
    }
}
