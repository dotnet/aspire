// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable IDE0005

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests.Pipelines;

public class DistributedApplicationPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoSteps_CompletesSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());

        await pipeline.ExecuteAsync(context);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleStep_ExecutesStep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var stepExecuted = false;
        pipeline.AddStep("step1", async (context) =>
        {
            stepExecuted = true;
            await Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.True(stepExecuted);
    }

    [Fact]
    public void PipelineStep_CanHaveTags()
    {
        // Test that PipelineStep has Tags property
        var step = new PipelineStep
        {
            Name = "test-step",
            Action = (ctx) => Task.CompletedTask,
            Tags = ["tag1", "tag2"]
        };

        Assert.Equal("test-step", step.Name);
        Assert.Equal(2, step.Tags.Count);
        Assert.Contains("tag1", step.Tags);
        Assert.Contains("tag2", step.Tags);
    }

    [Fact]
    public void PipelineStep_TagsDefaultToEmpty()
    {
        // Test that PipelineStep Tags property defaults to empty list
        var step = new PipelineStep
        {
            Name = "test-step",
            Action = (ctx) => Task.CompletedTask
        };

        Assert.NotNull(step.Tags);
        Assert.Empty(step.Tags);
    }

    [Fact]
    public void WellKnownPipelineTags_HasExpectedValues()
    {
        // Test that WellKnownPipelineTags has the expected constant values
        Assert.Equal("provision-infra", WellKnownPipelineTags.ProvisionInfrastructure);
        Assert.Equal("build-compute", WellKnownPipelineTags.BuildCompute);
        Assert.Equal("deploy-compute", WellKnownPipelineTags.DeployCompute);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleIndependentSteps_ExecutesAllSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step1"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step2"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step3", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step3"); }
            await Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(3, executedSteps.Count);
        Assert.Contains("step1", executedSteps);
        Assert.Contains("step2", executedSteps);
        Assert.Contains("step3", executedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithDependsOn_ExecutesInOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            executedSteps.Add("step1");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            executedSteps.Add("step2");
            await Task.CompletedTask;
        }, dependsOn: "step1");

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
            await Task.CompletedTask;
        }, dependsOn: "step2");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(["step1", "step2", "step3"], executedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithRequiredBy_ExecutesInCorrectOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step1"); }
            await Task.CompletedTask;
        }, requiredBy: "step2");

        pipeline.AddStep("step2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step2"); }
            await Task.CompletedTask;
        }, requiredBy: "step3");

        pipeline.AddStep("step3", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step3"); }
            await Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(["step1", "step2", "step3"], executedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedDependsOnAndRequiredBy_ExecutesInCorrectOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step1"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step2"); }
            await Task.CompletedTask;
        }, requiredBy: "step3");

        pipeline.AddStep("step3", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step3"); }
            await Task.CompletedTask;
        }, dependsOn: "step1");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(3, executedSteps.Count);
        var step1Index = executedSteps.IndexOf("step1");
        var step2Index = executedSteps.IndexOf("step2");
        var step3Index = executedSteps.IndexOf("step3");

        Assert.True(step1Index < step3Index, "step1 should execute before step3");
        Assert.True(step2Index < step3Index, "step2 should execute before step3");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleLevels_ExecutesLevelsInOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executionOrder = new List<(string step, DateTime time)>();
        var executionOrderLock = new object();

        pipeline.AddStep("level1-step1", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add(("level1-step1", DateTime.UtcNow)); }
            await Task.Delay(10);
        });

        pipeline.AddStep("level1-step2", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add(("level1-step2", DateTime.UtcNow)); }
            await Task.Delay(10);
        });

        pipeline.AddStep("level2-step1", (context) =>
        {
            lock (executionOrder) { executionOrder.Add(("level2-step1", DateTime.UtcNow)); }
            return Task.CompletedTask;
        }, dependsOn: "level1-step1");

        pipeline.AddStep("level2-step2", (context) =>
        {
            lock (executionOrder) { executionOrder.Add(("level2-step2", DateTime.UtcNow)); }
            return Task.CompletedTask;
        }, dependsOn: "level1-step2");

        pipeline.AddStep("level3-step1", (context) =>
        {
            lock (executionOrder) { executionOrder.Add(("level3-step1", DateTime.UtcNow)); }
            return Task.CompletedTask;
        }, dependsOn: "level2-step1");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(5, executionOrder.Count);

        // With readiness-based scheduling, we only guarantee that dependencies are respected,
        // not that all steps at a given "level" complete before the next "level" starts.
        // Verify that each step starts after its direct dependencies.
        var stepTimes = executionOrder.ToDictionary(x => x.step, x => x.time);

        Assert.True(stepTimes["level2-step1"] >= stepTimes["level1-step1"],
            "level2-step1 should start after level1-step1");
        Assert.True(stepTimes["level2-step2"] >= stepTimes["level1-step2"],
            "level2-step2 should start after level1-step2");
        Assert.True(stepTimes["level3-step1"] >= stepTimes["level2-step1"],
            "level3-step1 should start after level2-step1");
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineStepFactoryAnnotation_ExecutesAnnotatedSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) =>
                {
                    lock (executedSteps) { executedSteps.Add("annotated-step"); }
                    await Task.CompletedTask;
                }
            });

        var pipeline = new DistributedApplicationPipeline();
        pipeline.AddStep("regular-step", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("regular-step"); }
            await Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(2, executedSteps.Count);
        Assert.Contains("annotated-step", executedSteps);
        Assert.Contains("regular-step", executedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePipelineStepAnnotations_ExecutesAllAnnotatedSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) =>
            [
                new PipelineStep
                {
                    Name = "annotated-step-1",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("annotated-step-1"); }
                        await Task.CompletedTask;
                    }
                },
                new PipelineStep
                {
                    Name = "annotated-step-2",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("annotated-step-2"); }
                        await Task.CompletedTask;
                    }
                }
            ]);

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(2, executedSteps.Count);
        Assert.Contains("annotated-step-1", executedSteps);
        Assert.Contains("annotated-step-2", executedSteps);
    }

    [Fact]
    public void AddStep_WithDuplicateStepNames_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) => await Task.CompletedTask);

        var ex = Assert.Throws<InvalidOperationException>(() => pipeline.AddStep("step1", async (context) => await Task.CompletedTask));
        Assert.Contains("A step with the name 'step1' has already been added", ex.Message);
        Assert.Contains("step1", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownDependency_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) => await Task.CompletedTask, dependsOn: "unknown-step");

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("depends on unknown step", ex.Message);
        Assert.Contains("unknown-step", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownRequiredBy_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) => await Task.CompletedTask, requiredBy: "unknown-step");

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("required by unknown step", ex.Message);
        Assert.Contains("unknown-step", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCircularDependency_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var step1 = new PipelineStep
        {
            Name = "step1",
            Action = async (context) => await Task.CompletedTask
        };
        step1.DependsOn("step2");

        var step2 = new PipelineStep
        {
            Name = "step2",
            Action = async (context) => await Task.CompletedTask
        };
        step2.DependsOn("step1");

        pipeline.AddStep(step1);
        pipeline.AddStep(step2);

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Circular dependency", ex.Message);
        Assert.Contains("step1", ex.Message);
        Assert.Contains("step2", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepThrows_WrapsExceptionWithStepName()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var exceptionMessage = "Test exception";
        pipeline.AddStep("failing-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new NotSupportedException(exceptionMessage);
        });

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("failing-step", ex.Message);
        Assert.Contains("failed", ex.Message);
        Assert.NotNull(ex.InnerException);
        Assert.Equal(exceptionMessage, ex.InnerException.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexDependencyGraph_ExecutesInCorrectOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();

        pipeline.AddStep("a", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("a"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("b", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("b"); }
            await Task.CompletedTask;
        }, dependsOn: "a");

        pipeline.AddStep("c", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("c"); }
            await Task.CompletedTask;
        }, dependsOn: "a");

        pipeline.AddStep("d", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("d"); }
            await Task.CompletedTask;
        }, dependsOn: "b", requiredBy: "e");

        pipeline.AddStep("e", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("e"); }
            await Task.CompletedTask;
        }, dependsOn: "c");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(5, executedSteps.Count);

        var aIndex = executedSteps.IndexOf("a");
        var bIndex = executedSteps.IndexOf("b");
        var cIndex = executedSteps.IndexOf("c");
        var dIndex = executedSteps.IndexOf("d");
        var eIndex = executedSteps.IndexOf("e");

        Assert.True(aIndex < bIndex, "a should execute before b");
        Assert.True(aIndex < cIndex, "a should execute before c");
        Assert.True(bIndex < dIndex, "b should execute before d");
        Assert.True(cIndex < eIndex, "c should execute before e");
        Assert.True(dIndex < eIndex, "d should execute before e (requiredBy relationship)");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleDependencies_ExecutesInCorrectOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step1"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step2"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step3", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step3"); }
            await Task.CompletedTask;
        }, dependsOn: new[] { "step1", "step2" });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        var step1Index = executedSteps.IndexOf("step1");
        var step2Index = executedSteps.IndexOf("step2");
        var step3Index = executedSteps.IndexOf("step3");

        Assert.True(step1Index < step3Index, "step1 should execute before step3");
        Assert.True(step2Index < step3Index, "step2 should execute before step3");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleRequiredBy_ExecutesInCorrectOrder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();
        pipeline.AddStep("step1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step1"); }
            await Task.CompletedTask;
        }, requiredBy: new[] { "step2", "step3" });

        pipeline.AddStep("step2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step2"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("step3", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("step3"); }
            await Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        var step1Index = executedSteps.IndexOf("step1");
        var step2Index = executedSteps.IndexOf("step2");
        var step3Index = executedSteps.IndexOf("step3");

        Assert.True(step1Index < step2Index, "step1 should execute before step2");
        Assert.True(step1Index < step3Index, "step1 should execute before step3");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownRequiredByStep_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) =>
        {
            await Task.CompletedTask;
        }, requiredBy: "unknown-step");

        var context = CreateDeployingContext(builder.Build());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Step 'step1' is required by unknown step 'unknown-step'", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownRequiredByStepInList_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) =>
        {
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            await Task.CompletedTask;
        }, requiredBy: new[] { "step1", "unknown-step" });

        var context = CreateDeployingContext(builder.Build());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Step 'step2' is required by unknown step 'unknown-step'", exception.Message);
    }

    [Fact]
    public void AddStep_WithInvalidDependsOnType_ThrowsArgumentException()
    {
        var pipeline = new DistributedApplicationPipeline();

        var exception = Assert.Throws<ArgumentException>(() =>
            pipeline.AddStep("step1", async (context) => await Task.CompletedTask, dependsOn: 123));

        Assert.Contains("The dependsOn parameter must be a string or IEnumerable<string>", exception.Message);
    }

    [Fact]
    public void AddStep_WithInvalidRequiredByType_ThrowsArgumentException()
    {
        var pipeline = new DistributedApplicationPipeline();

        var exception = Assert.Throws<ArgumentException>(() =>
            pipeline.AddStep("step1", async (context) => await Task.CompletedTask, requiredBy: 123));

        Assert.Contains("The requiredBy parameter must be a string or IEnumerable<string>", exception.Message);
    }

    [Fact]
    public void AddStep_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("step1", async (context) => await Task.CompletedTask);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            pipeline.AddStep("step1", async (context) => await Task.CompletedTask));

        Assert.Contains("A step with the name 'step1' has already been added to the pipeline", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateAnnotationStepNames_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var resource1 = builder.AddResource(new CustomResource("resource1"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "duplicate-step",
                Action = async (ctx) => await Task.CompletedTask
            });

        var resource2 = builder.AddResource(new CustomResource("resource2"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "duplicate-step",
                Action = async (ctx) => await Task.CompletedTask
            });

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Duplicate step name", exception.Message);
        Assert.Contains("duplicate-step", exception.Message);
    }

    // Test for multiple failing steps at the same level removed due to inherent race conditions.
    // See https://github.com/dotnet/aspire/issues/12200

    [Fact]
    public async Task ExecuteAsync_WithFailingStep_PreservesOriginalStackTrace()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("failing-step", async (context) =>
        {
            await Task.CompletedTask;
            ThrowHelperMethod();
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("failing-step", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Contains("ThrowHelperMethod", exception.InnerException.StackTrace);
    }

    [Fact]
    public async Task PublishAsync_Deploy_WithNoResourcesAndNoPipelineSteps_ReturnsError()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");

        // Act
        await publisher.PublishAsync(app.Services.GetRequiredService<DistributedApplicationModel>(), CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundErrorActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                activity.Data.IsError &&
                activity.Data.CompletionMessage == "No deployment steps found in the application pipeline.")
            {
                foundErrorActivity = true;
                break;
            }
        }

        Assert.True(foundErrorActivity, "Expected to find a task activity with error about no deployment steps found");
    }

    [Fact]
    public async Task PublishAsync_Deploy_WithNoResourcesButHasPipelineSteps_Succeeds()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();
        pipeline.AddStep("test-step", async (context) => await Task.CompletedTask);

        builder.Services.AddSingleton<IDistributedApplicationPipeline>(pipeline);

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundSuccessActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                !activity.Data.IsError &&
                activity.Data.CompletionMessage == "Found deployment steps in the application pipeline.")
            {
                foundSuccessActivity = true;
                break;
            }
        }

        Assert.True(foundSuccessActivity, "Expected to find a task activity with message about deployment steps in the application pipeline");
    }

    [Fact]
    public async Task PublishAsync_Deploy_WithResourcesAndPipelineSteps_ShowsStepsMessage()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) => await Task.CompletedTask
            });

        var pipeline = new DistributedApplicationPipeline();
        pipeline.AddStep("direct-step", async (context) => await Task.CompletedTask);

        builder.Services.AddSingleton<IDistributedApplicationPipeline>(pipeline);

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundSuccessActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                !activity.Data.IsError &&
                activity.Data.CompletionMessage == "Found deployment steps in the application pipeline.")
            {
                foundSuccessActivity = true;
                break;
            }
        }

        Assert.True(foundSuccessActivity, "Expected to find a task activity with message about deployment steps in the application pipeline");
    }

    [Fact]
    public async Task PublishAsync_Deploy_WithOnlyResources_ShowsStepsMessage()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) => await Task.CompletedTask
            });

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundSuccessActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                !activity.Data.IsError &&
                activity.Data.CompletionMessage == "Found deployment steps in the application pipeline.")
            {
                foundSuccessActivity = true;
                break;
            }
        }

        Assert.True(foundSuccessActivity, "Expected to find a task activity with message about deployment steps in the application pipeline");
    }

    [Fact]
    public async Task PublishAsync_Publish_WithNoResources_ReturnsError()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: false);

        builder.Services.Configure<PublishingOptions>(options =>
        {
            options.OutputPath = Path.GetTempPath();
        });

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundErrorActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                activity.Data.IsError &&
                activity.Data.CompletionMessage == "No resources in the distributed application model support publishing.")
            {
                foundErrorActivity = true;
                break;
            }
        }

        Assert.True(foundErrorActivity, "Expected to find a task activity with error about no resources supporting publishing");
    }

    [Fact]
    public async Task PublishAsync_Publish_WithResources_ShowsResourceCount()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: false);

        builder.Services.Configure<PublishingOptions>(options =>
        {
            options.OutputPath = Path.GetTempPath();
        });

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithAnnotation(new PublishingCallbackAnnotation(async (context) => await Task.CompletedTask));

        var app = builder.Build();
        var publisher = app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("default");
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        await publisher.PublishAsync(model, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundSuccessActivity = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Task &&
                !activity.Data.IsError &&
                activity.Data.CompletionMessage?.StartsWith("Found 1 resources that support publishing.") == true)
            {
                foundSuccessActivity = true;
                break;
            }
        }

        Assert.True(foundSuccessActivity, "Expected to find a task activity with message about resources supporting publishing");
    }

    private static void ThrowHelperMethod()
    {
        throw new NotSupportedException("Test exception for stack trace");
    }

    [Fact]
    public async Task ExecuteAsync_WithDependencyFailure_ReportsFailedDependency()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var dependentStepExecuted = false;

        // Step that will fail
        pipeline.AddStep("failing-dependency", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Dependency failed");
        });

        // Step that depends on the failing step
        pipeline.AddStep("dependent-step", async (context) =>
        {
            dependentStepExecuted = true;
            await Task.CompletedTask;
        }, dependsOn: "failing-dependency");

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // The dependent step should not have executed
        Assert.False(dependentStepExecuted, "Dependent step should not execute when dependency fails");

        // The error message should indicate which dependency failed
        Assert.Contains("failing-dependency", ex.Message);
        Assert.Contains("failed", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCircularDependencyInComplex_ThrowsInvalidOperationException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        // Create a more complex circular dependency: A -> B -> C -> A
        var stepA = new PipelineStep
        {
            Name = "stepA",
            Action = async (context) => await Task.CompletedTask
        };
        stepA.DependsOn("stepC");

        var stepB = new PipelineStep
        {
            Name = "stepB",
            Action = async (context) => await Task.CompletedTask
        };
        stepB.DependsOn("stepA");

        var stepC = new PipelineStep
        {
            Name = "stepC",
            Action = async (context) => await Task.CompletedTask
        };
        stepC.DependsOn("stepB");

        pipeline.AddStep(stepA);
        pipeline.AddStep(stepB);
        pipeline.AddStep(stepC);

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Circular dependency", ex.Message);
        // Should mention the cycle
        Assert.True(ex.Message.Contains("stepA") || ex.Message.Contains("stepB") || ex.Message.Contains("stepC"),
            "Error message should mention at least one step in the cycle");
    }

    [Fact]
    public async Task ExecuteAsync_WithFailure_PreventsOtherStepsFromStarting()
    {
        // Test that when one step fails, other steps that haven't started yet don't start
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var step2Started = false;

        // Step 1 will fail after a short delay
        pipeline.AddStep("step1", async (context) =>
        {
            await Task.Delay(50);
            throw new InvalidOperationException("Step 1 failed");
        });

        // Step 2 depends on step1, so it definitely shouldn't start
        pipeline.AddStep("step2", async (context) =>
        {
            step2Started = true;
            await Task.CompletedTask;
        }, dependsOn: "step1");

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // Step 2 should never start because its dependency failed
        Assert.False(step2Started, "Step depending on failed step should not start");
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepThrows_ReportsFailureToActivityReporter()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();
        var exceptionMessage = "Test exception for reporting";
        pipeline.AddStep("failing-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new NotSupportedException(exceptionMessage);
        });

        var context = CreateDeployingContext(builder.Build());

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // Assert - Verify the exception was thrown
        Assert.Contains("failing-step", ex.Message);
        Assert.Contains("failed", ex.Message);

        // Assert - Verify the step was reported as failed
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var foundFailedStep = false;

        while (activityReader.TryRead(out var activity))
        {
            if (activity.Type == PublishingActivityTypes.Step &&
                activity.Data.IsError &&
                activity.Data.StatusText?.Contains("failing-step") == true)
            {
                foundFailedStep = true;
                break;
            }
        }

        Assert.True(foundFailedStep, "Expected to find a step activity marked as failed with error state");
    }

    [Fact]
    public async Task ExecuteAsync_WithDiamondDependency_ExecutesCorrectly()
    {
        // Diamond pattern: A -> B, A -> C, B -> D, C -> D
        // D should only start after both B and C complete
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executionOrder = new List<string>();
        var executionTimes = new Dictionary<string, DateTime>();

        pipeline.AddStep("A", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add("A"); executionTimes["A"] = DateTime.UtcNow; }
            await Task.Delay(10);
        });

        pipeline.AddStep("B", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add("B"); executionTimes["B"] = DateTime.UtcNow; }
            await Task.Delay(10);
        }, dependsOn: "A");

        pipeline.AddStep("C", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add("C"); executionTimes["C"] = DateTime.UtcNow; }
            await Task.Delay(10);
        }, dependsOn: "A");

        pipeline.AddStep("D", async (context) =>
        {
            lock (executionOrder) { executionOrder.Add("D"); executionTimes["D"] = DateTime.UtcNow; }
            await Task.CompletedTask;
        }, dependsOn: new[] { "B", "C" });

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(4, executionOrder.Count);

        // Verify execution order
        var aIndex = executionOrder.IndexOf("A");
        var bIndex = executionOrder.IndexOf("B");
        var cIndex = executionOrder.IndexOf("C");
        var dIndex = executionOrder.IndexOf("D");

        Assert.True(aIndex < bIndex, "A should execute before B");
        Assert.True(aIndex < cIndex, "A should execute before C");
        Assert.True(bIndex < dIndex, "B should execute before D");
        Assert.True(cIndex < dIndex, "C should execute before D");

        // Verify that D started after both B and C (not just one of them)
        Assert.True(executionTimes["D"] >= executionTimes["B"], "D should start after B completes");
        Assert.True(executionTimes["D"] >= executionTimes["C"], "D should start after C completes");
    }

    private static PipelineContext CreateDeployingContext(DistributedApplication app)
    {
        return new PipelineContext(
            app.Services.GetRequiredService<DistributedApplicationModel>(),
            app.Services.GetRequiredService<DistributedApplicationExecutionContext>(),
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: null);
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineStepFactoryAnnotation_FactoryReceivesPipelineContextAndResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        IResource? capturedResource = null;
        PipelineContext? capturedPipelineContext = null;
        var executedSteps = new List<string>();

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) =>
            {
                capturedResource = factoryContext.Resource;
                capturedPipelineContext = factoryContext.PipelineContext;

                return new PipelineStep
                {
                    Name = "annotated-step",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("annotated-step"); }
                        await Task.CompletedTask;
                    }
                };
            });

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.NotNull(capturedResource);
        Assert.Equal("test-resource", capturedResource.Name);
        Assert.NotNull(capturedPipelineContext);
        Assert.Same(context, capturedPipelineContext);
        Assert.Contains("annotated-step", executedSteps);
    }

    [Fact]
    public async Task WithPipelineStepFactory_SyncOverload_ExecutesStep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) => new PipelineStep
            {
                Name = "sync-step",
                Action = async (ctx) =>
                {
                    lock (executedSteps) { executedSteps.Add("sync-step"); }
                    await Task.CompletedTask;
                }
            });

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Contains("sync-step", executedSteps);
    }

    [Fact]
    public async Task WithPipelineStepFactory_AsyncOverload_ExecutesStep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory(async (factoryContext) =>
            {
                await Task.CompletedTask;
                return new PipelineStep
                {
                    Name = "async-step",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("async-step"); }
                        await Task.CompletedTask;
                    }
                };
            });

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Contains("async-step", executedSteps);
    }

    [Fact]
    public async Task WithPipelineStepFactory_MultipleStepsSyncOverload_ExecutesAllSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory((factoryContext) =>
            [
                new PipelineStep
                {
                    Name = "sync-step-1",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("sync-step-1"); }
                        await Task.CompletedTask;
                    }
                },
                new PipelineStep
                {
                    Name = "sync-step-2",
                    Action = async (ctx) =>
                    {
                        lock (executedSteps) { executedSteps.Add("sync-step-2"); }
                        await Task.CompletedTask;
                    }
                }
            ]);

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Contains("sync-step-1", executedSteps);
        Assert.Contains("sync-step-2", executedSteps);
    }

    [Fact]
    public async Task WithPipelineStepFactory_MultipleStepsAsyncOverload_ExecutesAllSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithPipelineStepFactory(async (factoryContext) =>
            {
                await Task.CompletedTask;
                return
                [
                    new PipelineStep
                    {
                        Name = "async-step-1",
                        Action = async (ctx) =>
                        {
                            lock (executedSteps) { executedSteps.Add("async-step-1"); }
                            await Task.CompletedTask;
                        }
                    },
                    new PipelineStep
                    {
                        Name = "async-step-2",
                        Action = async (ctx) =>
                        {
                            lock (executedSteps) { executedSteps.Add("async-step-2"); }
                            await Task.CompletedTask;
                        }
                    }
                ];
            });

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Contains("async-step-1", executedSteps);
        Assert.Contains("async-step-2", executedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineLoggerProvider_LogsToStepLogger()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();
        var loggedMessages = new List<string>();

        pipeline.AddStep("logging-step", (context) =>
        {
            // Get a logger from DI which should be the PipelineLogger
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("TestCategory");

            logger.LogInformation("Test log message from pipeline step");
            return Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert

        // Collect all activities for easier assertion
        var activities = new List<PublishingActivity>();
        while (reporter.ActivityItemUpdated.Reader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        var stepActivities = activities.Where(a => a.Type == PublishingActivityTypes.Step).GroupBy(a => a.Data.Id).ToList();
        var logActivities = activities.Where(a => a.Type == PublishingActivityTypes.Log).ToList();

        var stepActivity = Assert.Single(stepActivities);
        Assert.Collection(stepActivity,
            step =>
            {
                Assert.Equal("logging-step", step.Data.StatusText);
                Assert.False(step.Data.IsComplete);
            },
            step =>
            {
                Assert.True(step.Data.IsComplete);
            });
        var logActivity = Assert.Single(logActivities);
        Assert.Equal("Test log message from pipeline step", logActivity.Data.StatusText);
        Assert.Equal("Information", logActivity.Data.LogLevel);
        Assert.Equal(stepActivities[0].First().Data.Id, logActivity.Data.StepId);
        Assert.False(logActivity.Data.EnableMarkdown);
    }

    [Fact]
    public async Task ExecuteAsync_PipelineLoggerProvider_IsolatesLoggingBetweenSteps()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();
        var step1Logger = (ILogger?)null;
        var step2Logger = (ILogger?)null;

        pipeline.AddStep("step1", async (context) =>
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            step1Logger = loggerFactory.CreateLogger("Step1Category");

            // Verify this step has its own contextual logger
            Assert.Same(context.Logger, PipelineLoggerProvider.CurrentLogger);

            step1Logger.LogInformation("Message from step 1");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", (context) =>
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            step2Logger = loggerFactory.CreateLogger("Step2Category");

            // Verify this step has its own contextual logger (different from step1)
            Assert.Same(context.Logger, PipelineLoggerProvider.CurrentLogger);

            step2Logger.LogInformation("Message from step 2");
            return Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert
        Assert.NotNull(step1Logger);
        Assert.NotNull(step2Logger);

        // Collect all activities for easier assertion
        var activities = new List<PublishingActivity>();
        while (reporter.ActivityItemUpdated.Reader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        var stepOrder = new[] { "step1", "step2" };
        var logOrder = new[] { "Message from step 1", "Message from step 2" };

        var stepActivities = activities.Where(a => a.Type == PublishingActivityTypes.Step)
            .GroupBy(a => a.Data.Id)
            .OrderBy(g => Array.IndexOf(stepOrder, g.First().Data.StatusText))
            .ToList();
        var logActivities = activities.Where(a => a.Type == PublishingActivityTypes.Log)
            .OrderBy(a => Array.IndexOf(logOrder, a.Data.StatusText))
            .ToList();

        Assert.Collection(stepActivities,
            step1Activity =>
            {
                Assert.Collection(step1Activity,
                    step =>
                    {
                        Assert.Equal("step1", step.Data.StatusText);
                        Assert.False(step.Data.IsComplete);
                    },
                    step =>
                    {
                        Assert.True(step.Data.IsComplete);
                    });
            },
            step2Activity =>
            {
                Assert.Collection(step2Activity,
                    step =>
                    {
                        Assert.Equal("step2", step.Data.StatusText);
                        Assert.False(step.Data.IsComplete);
                    },
                    step =>
                    {
                        Assert.True(step.Data.IsComplete);
                    });
            });

        Assert.Collection(logActivities,
            logActivity =>
            {
                Assert.Equal("Message from step 1", logActivity.Data.StatusText);
                Assert.Equal("Information", logActivity.Data.LogLevel);
                var step1ActivityGroup = stepActivities.First(g => g.First().Data.StatusText == "step1");
                Assert.Equal(step1ActivityGroup.First().Data.Id, logActivity.Data.StepId);
            },
            logActivity =>
            {
                Assert.Equal("Message from step 2", logActivity.Data.StatusText);
                Assert.Equal("Information", logActivity.Data.LogLevel);
                var step2ActivityGroup = stepActivities.First(g => g.First().Data.StatusText == "step2");
                Assert.Equal(step2ActivityGroup.First().Data.Id, logActivity.Data.StepId);
            });

        // After execution, current logger should be NullLogger
        Assert.Same(NullLogger.Instance, PipelineLoggerProvider.CurrentLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepFails_PipelineLoggerIsCleanedUp()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("failing-step", async (context) =>
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("FailingCategory");

            logger.LogInformation("About to fail");

            throw new InvalidOperationException("Test failure");
        });

        var context = CreateDeployingContext(builder.Build());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // Collect all activities for easier assertion
        var activities = new List<PublishingActivity>();
        while (reporter.ActivityItemUpdated.Reader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        var stepActivities = activities.Where(a => a.Type == PublishingActivityTypes.Step).GroupBy(a => a.Data.Id).ToList();
        var logActivities = activities.Where(a => a.Type == PublishingActivityTypes.Log).ToList();

        Assert.Collection(stepActivities,
            stepActivity =>
            {
                Assert.Collection(stepActivity,
                    step =>
                    {
                        Assert.Equal("failing-step", step.Data.StatusText);
                        Assert.False(step.Data.IsComplete);
                    },
                    step =>
                    {
                        Assert.True(step.Data.IsError);
                    });
            });

        var logActivity = Assert.Single(logActivities);
        Assert.Equal("About to fail", logActivity.Data.StatusText);
        Assert.Equal("Information", logActivity.Data.LogLevel);
        Assert.Equal(stepActivities[0].First().Data.Id, logActivity.Data.StepId);

        // Verify logger is cleaned up even after failure
        Assert.Same(NullLogger.Instance, PipelineLoggerProvider.CurrentLogger);
    }

    [Fact]
    public async Task ExecuteAsync_PipelineLoggerProvider_PreservesLoggerAfterStepCompletion()
    {
        // This test verifies that each step gets a clean logger context
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();
        var capturedLoggers = new List<ILogger>();

        for (var i = 1; i <= 3; i++)
        {
            var stepNumber = i; // Capture for closure
            pipeline.AddStep($"step{stepNumber}", (context) =>
            {
                // Capture the current logger for this step
                lock (capturedLoggers)
                {
                    capturedLoggers.Add(PipelineLoggerProvider.CurrentLogger);
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger($"Step{stepNumber}");

                logger.LogInformation("Executing step {stepNumber}", stepNumber);
                return Task.CompletedTask;
            });
        }

        var context = CreateDeployingContext(builder.Build());

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert
        Assert.Equal(3, capturedLoggers.Count);

        // Each step should have had a different logger context
        // (We can't easily verify they're different instances since they're created per step,
        // but we can verify none of them are NullLogger during execution)
        foreach (var logger in capturedLoggers)
        {
            Assert.NotSame(NullLogger.Instance, logger);
        }

        // Collect all activities for easier assertion
        var activities = new List<PublishingActivity>();
        while (reporter.ActivityItemUpdated.Reader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        var stepOrder = new[] { "step1", "step2", "step3" };
        var logOrder = new[] { "Executing step 1", "Executing step 2", "Executing step 3" };

        var stepActivities = activities.Where(a => a.Type == PublishingActivityTypes.Step)
            .GroupBy(a => a.Data.Id)
            .OrderBy(g => Array.IndexOf(stepOrder, g.First().Data.StatusText))
            .ToList();
        var logActivities = activities.Where(a => a.Type == PublishingActivityTypes.Log)
            .OrderBy(a => Array.IndexOf(logOrder, a.Data.StatusText))
            .ToList();

        Assert.Equal(3, stepActivities.Count);
        Assert.Collection(logActivities,
            logActivity =>
            {
                Assert.Equal("Executing step 1", logActivity.Data.StatusText);
                Assert.Equal("Information", logActivity.Data.LogLevel);
            },
            logActivity =>
            {
                Assert.Equal("Executing step 2", logActivity.Data.StatusText);
                Assert.Equal("Information", logActivity.Data.LogLevel);
            },
            logActivity =>
            {
                Assert.Equal("Executing step 3", logActivity.Data.StatusText);
                Assert.Equal("Information", logActivity.Data.LogLevel);
            });

        // Verify each log activity is associated with the correct step
        foreach (var logActivity in logActivities)
        {
            Assert.Contains(stepActivities, stepGroup => stepGroup.First().Data.Id == logActivity.Data.StepId);
        }

        // After all steps complete, should be back to NullLogger
        Assert.Same(NullLogger.Instance, PipelineLoggerProvider.CurrentLogger);
    }

    [Theory]
    [InlineData("Debug", new[] { "Debug", "Information", "Warning" }, new[] { "Debug", "Information", "Warning" })]
    [InlineData("Information", new[] { "Debug", "Information", "Warning" }, new[] { "Information", "Warning" })]
    [InlineData("Warning", new[] { "Debug", "Information", "Warning" }, new[] { "Warning" })]
    [InlineData("Error", new[] { "Debug", "Information", "Warning" }, new string[0])]
    public async Task ExecuteAsync_PipelineLoggerProvider_RespectsPublishingLogLevelConfiguration(
        string configuredLogLevel,
        string[] loggedLevels,
        string[] expectedFilteredLevels)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true, logLevel: configuredLogLevel);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PipelineActivityReporter(interactionService, NullLogger<PipelineActivityReporter>.Instance);

        builder.Services.AddSingleton<IPipelineActivityReporter>(reporter);

        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("logging-step", (context) =>
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("TestCategory");

            // Log messages at different levels
            foreach (var level in loggedLevels)
            {
                switch (level)
                {
                    case "Debug":
                        logger.LogDebug($"Debug message");
                        break;
                    case "Information":
                        logger.LogInformation($"Information message");
                        break;
                    case "Warning":
                        logger.LogWarning($"Warning message");
                        break;
                }
            }

            return Task.CompletedTask;
        });

        var context = CreateDeployingContext(builder.Build());

        // Act
        await pipeline.ExecuteAsync(context);

        // Assert
        var activities = new List<PublishingActivity>();
        while (reporter.ActivityItemUpdated.Reader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        var logActivities = activities.Where(a => a.Type == PublishingActivityTypes.Log).ToList();

        // Verify that only the expected log levels are present
        Assert.Equal(expectedFilteredLevels.Length, logActivities.Count);

        // Verify each expected log level appears exactly once
        foreach (var expectedLevel in expectedFilteredLevels)
        {
            Assert.Contains(logActivities, activity =>
                activity.Data.LogLevel == expectedLevel &&
                activity.Data.StatusText == $"{expectedLevel} message");
        }
    }

    private sealed class CustomResource(string name) : Resource(name)
    {
    }
}
