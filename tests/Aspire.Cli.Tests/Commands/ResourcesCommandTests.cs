// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class ResourcesCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ResourcesCommand_Help_Works()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_WhenNoAppHostRunning_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Should succeed - no running AppHost is not an error (like Unix ps with no processes)
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("Json")]
    [InlineData("JSON")]
    public async Task ResourcesCommand_FormatOption_IsCaseInsensitive(string format)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"resources --format {format} --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Theory]
    [InlineData("table")]
    [InlineData("Table")]
    [InlineData("TABLE")]
    public async Task ResourcesCommand_FormatOption_AcceptsTable(string format)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"resources --format {format} --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_FormatOption_RejectsInvalidValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources --format invalid");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.NotEqual(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_WatchOption_CanBeParsed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources --watch --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_WatchAndFormat_CanBeCombined()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources --watch --format json --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_ResourceNameArgument_CanBeParsed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources myresource --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ResourcesCommand_AllOptions_CanBeCombined()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("resources myresource --watch --format json --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public void ResourcesCommand_NdjsonFormat_OutputsOneObjectPerLine()
    {
        // Arrange - create resource JSON objects
        var resources = new[]
        {
            new ResourceJson { Name = "frontend", DisplayName = "frontend", ResourceType = "Project", State = "Running" },
            new ResourceJson { Name = "postgres", DisplayName = "postgres", ResourceType = "Container", State = "Running" },
            new ResourceJson { Name = "redis", DisplayName = "redis", ResourceType = "Container", State = "Starting" }
        };

        // Act - serialize each resource separately (simulating NDJSON streaming output for --watch)
        var ndjsonLines = resources
            .Select(r => System.Text.Json.JsonSerializer.Serialize(r, ResourcesCommandJsonContext.Ndjson.ResourceJson))
            .ToList();

        // Assert - each line is a complete, valid JSON object with no internal newlines
        foreach (var line in ndjsonLines)
        {
            // Verify no newlines within the JSON (compact format)
            Assert.DoesNotContain('\n', line);
            Assert.DoesNotContain('\r', line);

            // Verify it's valid JSON that can be deserialized
            var deserialized = System.Text.Json.JsonSerializer.Deserialize(line, ResourcesCommandJsonContext.Ndjson.ResourceJson);
            Assert.NotNull(deserialized);
        }

        // Verify NDJSON format: joining with newlines creates parseable multi-line output
        var ndjsonOutput = string.Join('\n', ndjsonLines);
        var parsedLines = ndjsonOutput.Split('\n')
            .Select(line => System.Text.Json.JsonSerializer.Deserialize(line, ResourcesCommandJsonContext.Ndjson.ResourceJson))
            .ToList();

        Assert.Equal(3, parsedLines.Count);
        Assert.Equal("frontend", parsedLines[0]!.Name);
        Assert.Equal("postgres", parsedLines[1]!.Name);
        Assert.Equal("Starting", parsedLines[2]!.State);
    }

    [Fact]
    public void ResourcesCommand_SnapshotFormat_OutputsWrappedJsonArray()
    {
        // Arrange - resources output for snapshot
        var resourcesOutput = new ResourcesOutput
        {
            Resources =
            [
                new ResourceJson { Name = "frontend", DisplayName = "frontend", ResourceType = "Project", State = "Running" },
                new ResourceJson { Name = "postgres", DisplayName = "postgres", ResourceType = "Container", State = "Running" }
            ]
        };

        // Act - serialize as snapshot (wrapped JSON)
        var json = System.Text.Json.JsonSerializer.Serialize(resourcesOutput, ResourcesCommandJsonContext.RelaxedEscaping.ResourcesOutput);

        // Assert - it's a single JSON object with "resources" array
        Assert.Contains("\"resources\"", json);
        Assert.StartsWith("{", json.TrimStart());
        Assert.EndsWith("}", json.TrimEnd());

        // Verify it can be deserialized back
        var deserialized = System.Text.Json.JsonSerializer.Deserialize(json, ResourcesCommandJsonContext.RelaxedEscaping.ResourcesOutput);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Resources.Length);
        Assert.Equal("frontend", deserialized.Resources[0].Name);
    }
}
