// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Publishing.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tests.Publishing;

public class DeploymentStateManagerTests
{
    [Fact]
    public async Task AcquireSectionAsync_ReturnsEmptySection_WhenStateIsNew()
    {
        var stateManager = CreateFileDeploymentStateManager();

        using var section = await stateManager.AcquireSectionAsync("Parameters");

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

        using (var section1 = await stateManager.AcquireSectionAsync("Parameters"))
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        using var section2 = await stateManager.AcquireSectionAsync("Parameters");

        Assert.Equal(1, section2.Version);
        Assert.Equal("value1", section2.Data["key1"]?.GetValue<string>());
    }

    [Fact]
    public async Task SaveSectionAsync_ThrowsException_WhenVersionConflictDetected()
    {
        var stateManager = CreateFileDeploymentStateManager();

        // Acquire and save first section
        DeploymentStateSection oldSection;
        using (var section1 = await stateManager.AcquireSectionAsync("Parameters"))
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
            // Create a copy of the section before disposing
            oldSection = new DeploymentStateSection(section1.SectionName, section1.Data, section1.Version, () => { });
        }

        // Acquire and save the section again, incrementing version
        using (var section2 = await stateManager.AcquireSectionAsync("Parameters"))
        {
            section2.Data["key2"] = "value2";
            await stateManager.SaveSectionAsync(section2);
        }

        // Try to save the old section - should throw due to version conflict
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stateManager.SaveSectionAsync(oldSection));

        Assert.Contains("Concurrency conflict detected in section 'Parameters'", exception.Message);
        Assert.Contains("Expected version 0", exception.Message);
    }

    [Fact]
    public async Task MultipleSections_CanBeModified_Independently()
    {
        var stateManager = CreateFileDeploymentStateManager();

        using (var parametersSection = await stateManager.AcquireSectionAsync("Parameters"))
        using (var azureSection = await stateManager.AcquireSectionAsync("Azure"))
        {
            parametersSection.Data["param1"] = "value1";
            azureSection.Data["resource1"] = "azure-value1";

            await stateManager.SaveSectionAsync(parametersSection);
            await stateManager.SaveSectionAsync(azureSection);
        }

        using var parametersCheck = await stateManager.AcquireSectionAsync("Parameters");
        using var azureCheck = await stateManager.AcquireSectionAsync("Azure");

        Assert.Equal(1, parametersCheck.Version);
        Assert.Equal(1, azureCheck.Version);
        Assert.Equal("value1", parametersCheck.Data["param1"]?.GetValue<string>());
        Assert.Equal("azure-value1", azureCheck.Data["resource1"]?.GetValue<string>());
    }

    [Fact]
    public async Task ConcurrentAccess_ToSameSection_IsSerializedByLock()
    {
        var stateManager = CreateFileDeploymentStateManager();
        var counter = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var section = await stateManager.AcquireSectionAsync("Parameters");
                var currentValue = counter;
                await Task.Delay(10);
                counter = currentValue + 1;
                section.Data["counter"] = counter;
                await stateManager.SaveSectionAsync(section);
            }));
        }

        await Task.WhenAll(tasks);

        using var finalSection = await stateManager.AcquireSectionAsync("Parameters");
        Assert.Equal(10, finalSection.Version);
    }

    [Fact]
    public async Task AcquireSectionAsync_UsesExclusiveLock_OnFirstLoad()
    {
        var stateManager = CreateFileDeploymentStateManager();

        using (var section1 = await stateManager.AcquireSectionAsync("Parameters"))
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        using var section2 = await stateManager.AcquireSectionAsync("Parameters");
        using var section3 = await stateManager.AcquireSectionAsync("Azure");

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

        using (var section1 = await stateManager.AcquireSectionAsync("Parameters"))
        {
            section1.Data["key1"] = "value1";
            await stateManager.SaveSectionAsync(section1);
        }

        var stateManager2 = CreateFileDeploymentStateManager(sharedSha);
        using (var section2 = await stateManager2.AcquireSectionAsync("Parameters"))
        {
            // Data persists across manager instances
            Assert.Equal("value1", section2.Data["key1"]?.GetValue<string>());

            // But version tracking is per-instance (starts at 0)
            Assert.Equal(0, section2.Version);

            section2.Data["key2"] = "value2";
            await stateManager2.SaveSectionAsync(section2);
        }

        var stateManager3 = CreateFileDeploymentStateManager(sharedSha);
        using var section3 = await stateManager3.AcquireSectionAsync("Parameters");

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

        var section1 = await stateManager.AcquireSectionAsync("Parameters");
        section1.Dispose();

        var section2 = await stateManager.AcquireSectionAsync("Parameters");
        section2.Dispose();

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

        using var parametersSection = await stateManager.AcquireSectionAsync("Parameters");

        Assert.Equal(0, parametersSection.Version);
    }

    [Fact]
    public async Task WithStateData_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act - Multiple threads accessing the DeploymentState concurrently via WithStateData
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    section.WithStateData(state =>
                    {
                        // All threads try to get or create the same "Azure" property
                        if (state["Azure"] is not JsonObject azureNode)
                        {
                            azureNode = new JsonObject();
                            state["Azure"] = azureNode;
                        }
                        else
                        {
                            azureNode = state["Azure"]!.AsObject();
                        }

                        // Each thread creates a unique property
                        var threadNode = azureNode[$"Thread{threadId}"] as JsonObject ?? new JsonObject();
                        azureNode[$"Thread{threadId}"] = threadNode;
                        threadNode["Counter"] = j;

                        // And a shared property under Azure
                        var deploymentsNode = azureNode["Deployments"] as JsonObject ?? new JsonObject();
                        azureNode["Deployments"] = deploymentsNode;

                        // Access a deeper nested property
                        var resourceKey = $"Resource{j % 5}";
                        var resourceNode = deploymentsNode[resourceKey] as JsonObject ?? new JsonObject();
                        deploymentsNode[resourceKey] = resourceNode;
                        resourceNode["LastAccess"] = $"Thread{threadId}-{j}";
                    });
                }
            });
        }

        // Assert - Should complete without exceptions
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        // Verify the structure was created correctly
        section.WithStateData(state =>
        {
            Assert.NotNull(state["Azure"]);
            var azureObj = state["Azure"]!.AsObject();
            Assert.NotNull(azureObj["Deployments"]);

            // Check that all thread-specific nodes were created
            for (int i = 0; i < threadCount; i++)
            {
                Assert.NotNull(azureObj[$"Thread{i}"]);
            }
        });
    }

    [Fact]
    public async Task WithStateData_ConcurrentReadsAndWrites_MaintainsConsistency()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");
        const int writerCount = 5;
        const int readerCount = 5;
        const int iterations = 100;

        // Initialize counter
        section.WithStateData(state => state["Counter"] = 0);

        var writerTasks = new Task[writerCount];
        var readerTasks = new Task[readerCount];

        // Act - Writers increment counter
        for (int i = 0; i < writerCount; i++)
        {
            writerTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    section.WithStateData(state =>
                    {
                        var current = state["Counter"]!.GetValue<int>();
                        state["Counter"] = current + 1;
                    });
                }
            });
        }

        // Readers read counter
        var readValues = new List<int>[readerCount];
        for (int i = 0; i < readerCount; i++)
        {
            int readerIndex = i;
            readValues[readerIndex] = new List<int>();
            readerTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var value = section.WithStateData(state =>
                    {
                        return state["Counter"]!.GetValue<int>();
                    });
                    readValues[readerIndex].Add(value);
                    Thread.Sleep(1); // Small delay to allow interleaving
                }
            });
        }

        await Task.WhenAll(writerTasks.Concat(readerTasks)).WaitAsync(TimeSpan.FromSeconds(15));

        // Assert - Final counter value should be exactly writerCount * iterations
        var finalValue = section.WithStateData(state =>
        {
            return state["Counter"]!.GetValue<int>();
        });

        Assert.Equal(writerCount * iterations, finalValue);

        // All read values should be in valid range (0 to finalValue)
        foreach (var readerValues in readValues)
        {
            Assert.All(readerValues, value =>
            {
                Assert.InRange(value, 0, finalValue);
            });
        }
    }

    [Fact]
    public async Task WithStateDataAsync_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");
        const int taskCount = 10;
        const int iterationsPerTask = 50;
        var tasks = new Task[taskCount];

        // Act - Multiple async tasks accessing the state concurrently via WithStateDataAsync
        for (int i = 0; i < taskCount; i++)
        {
            int taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    await section.WithStateDataAsync(async state =>
                    {
                        // Simulate some async work
                        await Task.Delay(1);

                        // Create task-specific data
                        var taskNode = state[$"Task{taskId}"] as JsonObject ?? new JsonObject();
                        state[$"Task{taskId}"] = taskNode;
                        taskNode["Iteration"] = j;
                        taskNode["Timestamp"] = DateTime.UtcNow.Ticks;
                    });
                }
            });
        }

        // Assert - Should complete without exceptions
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(15));

        // Verify all task nodes were created
        await section.WithStateDataAsync(async state =>
        {
            await Task.CompletedTask; // Make it async
            for (int i = 0; i < taskCount; i++)
            {
                Assert.NotNull(state[$"Task{i}"]);
                var taskNode = state[$"Task{i}"]!.AsObject();
                Assert.Equal(iterationsPerTask - 1, taskNode["Iteration"]!.GetValue<int>());
            }
        });
    }

    [Fact]
    public async Task WithStateDataAsync_WithReturnValue_MaintainsConsistency()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");
        const int taskCount = 20;
        var tasks = new Task<int>[taskCount];

        // Initialize counter
        await section.WithStateDataAsync(async state =>
        {
            await Task.CompletedTask;
            state["Counter"] = 0;
        });

        // Act - Multiple tasks incrementing and reading the counter
        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                return await section.WithStateDataAsync(async state =>
                {
                    await Task.Delay(1); // Simulate async work
                    var current = state["Counter"]!.GetValue<int>();
                    var newValue = current + 1;
                    state["Counter"] = newValue;
                    return newValue;
                });
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All returned values should be unique and in valid range
        Assert.Equal(taskCount, results.Distinct().Count());
        Assert.All(results, value => Assert.InRange(value, 1, taskCount));

        // Final counter value should be exactly taskCount
        var finalValue = await section.WithStateDataAsync(async state =>
        {
            await Task.CompletedTask;
            return state["Counter"]!.GetValue<int>();
        });

        Assert.Equal(taskCount, finalValue);
    }

    [Fact]
    public async Task WithStateData_NestedAccess_WorksCorrectly()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");

        // Act - Create a complex nested structure
        section.WithStateData(state =>
        {
            var azure = state["Azure"] as JsonObject ?? new JsonObject();
            state["Azure"] = azure;

            var resources = azure["Resources"] as JsonObject ?? new JsonObject();
            azure["Resources"] = resources;

            var storage = resources["Storage"] as JsonObject ?? new JsonObject();
            resources["Storage"] = storage;

            storage["AccountName"] = "mystorageaccount";
            storage["Location"] = "westus2";
            storage["Sku"] = "Standard_LRS";
        });

        // Assert - Verify the nested structure
        section.WithStateData(state =>
        {
            Assert.NotNull(state["Azure"]);
            var azure = state["Azure"]!.AsObject();
            Assert.NotNull(azure["Resources"]);
            var resources = azure["Resources"]!.AsObject();
            Assert.NotNull(resources["Storage"]);
            var storage = resources["Storage"]!.AsObject();
            Assert.Equal("mystorageaccount", storage["AccountName"]!.GetValue<string>());
            Assert.Equal("westus2", storage["Location"]!.GetValue<string>());
            Assert.Equal("Standard_LRS", storage["Sku"]!.GetValue<string>());
        });
    }

    [Fact]
    public async Task WithStateData_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => section.WithStateData((Action<JsonObject>)null!));
    }

    [Fact]
    public async Task WithStateData_WithReturnValue_ThrowsArgumentNullException_WhenFuncIsNull()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => section.WithStateData((Func<JsonObject, int>)null!));
    }

    [Fact]
    public async Task WithStateDataAsync_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => section.WithStateDataAsync((Func<JsonObject, Task>)null!));
    }

    [Fact]
    public async Task WithStateDataAsync_WithReturnValue_ThrowsArgumentNullException_WhenFuncIsNull()
    {
        // Arrange
        var stateManager = CreateFileDeploymentStateManager();
        using var section = await stateManager.AcquireSectionAsync("Azure");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => section.WithStateDataAsync((Func<JsonObject, Task<int>>)null!));
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
        var publishingOptions = Options.Create(new PublishingOptions());

        return new FileDeploymentStateManager(
            NullLogger<FileDeploymentStateManager>.Instance,
            configuration,
            hostEnvironment,
            publishingOptions);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
