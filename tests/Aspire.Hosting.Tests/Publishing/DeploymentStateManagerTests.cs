// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json.Nodes;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Pipelines.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tests.Pipelines;

public class DeploymentStateManagerTests
{
    [Fact]
    public async Task AcquireSectionAsync_ReturnsEmptySection_WhenStateIsNew()
    {
        var stateManager = CreateFileDeploymentStateManager();

        var section = await stateManager.AcquireSectionAsync("Parameters");

        Assert.NotNull(section);
        Assert.Equal("Parameters", section.SectionName);
        Assert.Equal(0, section.Version);
        Assert.NotNull(section.Data);
        Assert.Empty(section.Data);
    }

    [Fact]
    public async Task SaveSectionAsync_IncrementsVersion_AfterSave()
    {
        var stateManager = CreateFileDeploymentStateManager();

        var section1 = await stateManager.AcquireSectionAsync("Parameters");
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        var section2 = await stateManager.AcquireSectionAsync("Parameters");

        Assert.Equal(1, section2.Version);
        Assert.Equal("value1", section2.Data["key1"]?.GetValue<string>());
    }

    [Fact]
    public async Task SaveSectionAsync_ThrowsException_WhenVersionConflictDetected()
    {
        var stateManager = CreateFileDeploymentStateManager();

        // Acquire and save first section
        DeploymentStateSection oldSection;
        var section1 = await stateManager.AcquireSectionAsync("Parameters");
        {
            section1.Data["key1"] = "value1";
            var oldVersion = section1.Version; // Capture version before save
            await stateManager.SaveSectionAsync(section1);
            // Create a copy of the section with the old version to simulate a stale section
            oldSection = new DeploymentStateSection(section1.SectionName, section1.Data, oldVersion);
        }

        // Acquire and save the section again, incrementing version
        var section2 = await stateManager.AcquireSectionAsync("Parameters");
        {
            section2.Data["key2"] = "value2";
            await stateManager.SaveSectionAsync(section2);
        }

        // Try to save the old section - should throw due to version conflict
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stateManager.SaveSectionAsync(oldSection));

        Assert.Contains("Concurrency conflict detected in section 'Parameters'", exception.Message);
    }

    [Fact]
    public async Task MultipleSections_CanBeModified_Independently()
    {
        var stateManager = CreateFileDeploymentStateManager();

        var parametersSection = await stateManager.AcquireSectionAsync("Parameters");
        var azureSection = await stateManager.AcquireSectionAsync("Azure");
        {
            parametersSection.Data["param1"] = "value1";
            azureSection.Data["resource1"] = "azure-value1";

            await stateManager.SaveSectionAsync(parametersSection);
            await stateManager.SaveSectionAsync(azureSection);
        }

        var parametersCheck = await stateManager.AcquireSectionAsync("Parameters");
        var azureCheck = await stateManager.AcquireSectionAsync("Azure");

        Assert.Equal(1, parametersCheck.Version);
        Assert.Equal(1, azureCheck.Version);
        Assert.Equal("value1", parametersCheck.Data["param1"]?.GetValue<string>());
        Assert.Equal("azure-value1", azureCheck.Data["resource1"]?.GetValue<string>());
    }
    [Fact]
    public async Task ConcurrentSaves_ToDifferentSections_AreSerializedToStorage()
    {
        var sharedSha = Guid.NewGuid().ToString("N");
        var stateManager = CreateFileDeploymentStateManager(sharedSha);
        var tasks = new List<Task>();

        // Concurrently save to different sections
        for (int i = 0; i < 10; i++)
        {
            int sectionIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                var section = await stateManager.AcquireSectionAsync($"Section{sectionIndex}");
                section.Data[$"key{sectionIndex}"] = $"value{sectionIndex}";
                await stateManager.SaveSectionAsync(section);
            }));
        }

        await Task.WhenAll(tasks);

        // Verify all sections were saved correctly by loading with a new state manager
        var verifyManager = CreateFileDeploymentStateManager(sharedSha);
        for (int i = 0; i < 10; i++)
        {
            var section = await verifyManager.AcquireSectionAsync($"Section{i}");
            Assert.Equal($"value{i}", section.Data[$"key{i}"]?.GetValue<string>());
        }
    }

    [Fact]
    public async Task AcquireSectionAsync_UsesExclusiveLock_OnFirstLoad()
    {
        var stateManager = CreateFileDeploymentStateManager();

        var section1 = await stateManager.AcquireSectionAsync("Parameters");
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        var section2 = await stateManager.AcquireSectionAsync("Parameters");
        var section3 = await stateManager.AcquireSectionAsync("Azure");

        Assert.NotNull(section2.Data);
        Assert.Equal("value1", section2.Data["key1"]?.GetValue<string>());
        Assert.Equal(1, section2.Version);
        Assert.Equal(0, section3.Version);
    }

    [Fact]
    public async Task DataPersists_AcrossSessions_ButVersionsAreInstanceSpecific()
    {
        var sharedSha = Guid.NewGuid().ToString("N");
        var stateManager = CreateFileDeploymentStateManager(sharedSha);

        var section1 = await stateManager.AcquireSectionAsync("Parameters");
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        var stateManager2 = CreateFileDeploymentStateManager(sharedSha);
        var section2 = await stateManager2.AcquireSectionAsync("Parameters");
        {
            // Data persists across manager instances
            Assert.Equal("value1", section2.Data["key1"]?.GetValue<string>());

            // But version tracking is per-instance (starts at 0)
            Assert.Equal(0, section2.Version);

            section2.Data["key2"] = "value2";
            await stateManager2.SaveSectionAsync(section2);
        }

        var stateManager3 = CreateFileDeploymentStateManager(sharedSha);
        var section3 = await stateManager3.AcquireSectionAsync("Parameters");

        // Data from both sessions is present
        Assert.Equal("value1", section3.Data["key1"]?.GetValue<string>());
        Assert.Equal("value2", section3.Data["key2"]?.GetValue<string>());

        // Version starts at 0 for this new instance
        Assert.Equal(0, section3.Version);
    }

    [Fact]
    public async Task StateSection_Dispose_ReleasesLock()
    {
        var stateManager = CreateFileDeploymentStateManager();

        _ = await stateManager.AcquireSectionAsync("Parameters");

        var section2 = await stateManager.AcquireSectionAsync("Parameters");

        Assert.NotNull(section2);
    }

    [Fact]
    public async Task BackwardCompatibility_LoadsStateWithoutMetadata()
    {
        var stateManager = CreateFileDeploymentStateManager();

        var state = new JsonObject
        {
            ["Parameters:param1"] = "value1",
            ["Azure:resource1"] = "azure-value1"
        };

        await stateManager.SaveStateAsync(state);

        var parametersSection = await stateManager.AcquireSectionAsync("Parameters");

        Assert.Equal(0, parametersSection.Version);
    }

    private static FileDeploymentStateManager CreateFileDeploymentStateManager(string? sha = null)
    {
        // Use a unique SHA per test by default to avoid test interference,
        // but allow tests to share state by passing the same SHA
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppHost:PathSha256"] = sha ?? Guid.NewGuid().ToString("N")
            })
            .Build();

        var hostEnvironment = new TestHostEnvironment { EnvironmentName = "Development" };
        var pipelineOptions = Options.Create(new Hosting.Pipelines.PipelineOptions());

        return new FileDeploymentStateManager(
            NullLogger<FileDeploymentStateManager>.Instance,
            configuration,
            hostEnvironment,
            pipelineOptions);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
