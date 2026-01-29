// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TemplateGeneratorCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task TemplateCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("template --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task TemplateCommandWithoutArgumentsReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("template");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task TemplateCommandWithInvalidTypeReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("template invalid TestName");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task TemplateCommandHostingTypeCreatesFiles()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TestOutput");
        var result = command.Parse($"template hosting TestIntegration --output \"{outputPath}\"");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify files were created
        var hostingDir = Path.Combine(outputPath, "Aspire.Hosting.TestIntegration");
        Assert.True(Directory.Exists(hostingDir), $"Expected directory {hostingDir} to exist");
        Assert.True(File.Exists(Path.Combine(hostingDir, "README.md")), "README.md should exist");
        Assert.True(File.Exists(Path.Combine(hostingDir, "TestIntegrationHostingExtensions.cs")), "Extensions file should exist");
        Assert.True(File.Exists(Path.Combine(hostingDir, "TestIntegrationResource.cs")), "Resource file should exist");
    }

    [Fact]
    public async Task TemplateCommandClientTypeCreatesFiles()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TestOutput");
        var result = command.Parse($"template client TestIntegration --output \"{outputPath}\"");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify files were created
        var clientDir = Path.Combine(outputPath, "Aspire.TestIntegration");
        Assert.True(Directory.Exists(clientDir), $"Expected directory {clientDir} to exist");
        Assert.True(File.Exists(Path.Combine(clientDir, "README.md")), "README.md should exist");
        Assert.True(File.Exists(Path.Combine(clientDir, "TestIntegrationExtensions.cs")), "Extensions file should exist");
    }

    [Fact]
    public async Task TemplateCommandFullTypeCreatesBothFiles()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TestOutput");
        var result = command.Parse($"template full TestIntegration --output \"{outputPath}\"");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify both hosting and client files were created
        var hostingDir = Path.Combine(outputPath, "Aspire.Hosting.TestIntegration");
        var clientDir = Path.Combine(outputPath, "Aspire.TestIntegration");
        
        Assert.True(Directory.Exists(hostingDir), $"Expected hosting directory {hostingDir} to exist");
        Assert.True(Directory.Exists(clientDir), $"Expected client directory {clientDir} to exist");
        
        Assert.True(File.Exists(Path.Combine(hostingDir, "README.md")), "Hosting README.md should exist");
        Assert.True(File.Exists(Path.Combine(clientDir, "README.md")), "Client README.md should exist");
    }

    [Fact]
    public async Task TemplateCommandWithCustomNamespaceUsesProvidedNamespace()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TestOutput");
        var customNamespace = "MyCompany.CustomLib";
        var result = command.Parse($"template client TestIntegration --output \"{outputPath}\" --namespace \"{customNamespace}\"");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify custom namespace was used
        var clientDir = Path.Combine(outputPath, "Aspire.TestIntegration");
        var extensionsFile = Path.Combine(clientDir, "TestIntegrationExtensions.cs");
        
        Assert.True(File.Exists(extensionsFile), "Extensions file should exist");
        
        var fileContent = await File.ReadAllTextAsync(extensionsFile);
        Assert.Contains($"namespace {customNamespace};", fileContent);
    }
}
