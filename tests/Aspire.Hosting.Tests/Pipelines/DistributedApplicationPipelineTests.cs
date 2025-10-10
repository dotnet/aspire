// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable IDE0005

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task ExecuteAsync_WithMultipleIndependentSteps_ExecutesAllSteps()
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
        });

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
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
            executedSteps.Add("step1");
            await Task.CompletedTask;
        }, requiredBy: "step2");

        pipeline.AddStep("step2", async (context) =>
        {
            executedSteps.Add("step2");
            await Task.CompletedTask;
        }, requiredBy: "step3");

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
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
            executedSteps.Add("step1");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            executedSteps.Add("step2");
            await Task.CompletedTask;
        }, requiredBy: "step3");

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
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
        var level1Complete = new TaskCompletionSource();
        var level2Complete = new TaskCompletionSource();

        pipeline.AddStep("level1-step1", async (context) =>
        {
            executionOrder.Add(("level1-step1", DateTime.UtcNow));
            await Task.Delay(10);
            await Task.CompletedTask;
        });

        pipeline.AddStep("level1-step2", async (context) =>
        {
            executionOrder.Add(("level1-step2", DateTime.UtcNow));
            await Task.Delay(10);
            await Task.CompletedTask;
        });

        pipeline.AddStep("level2-step1", async (context) =>
        {
            executionOrder.Add(("level2-step1", DateTime.UtcNow));
            await Task.CompletedTask;
        }, dependsOn: "level1-step1");

        pipeline.AddStep("level2-step2", async (context) =>
        {
            executionOrder.Add(("level2-step2", DateTime.UtcNow));
            await Task.CompletedTask;
        }, dependsOn: "level1-step2");

        pipeline.AddStep("level3-step1", async (context) =>
        {
            executionOrder.Add(("level3-step1", DateTime.UtcNow));
            await Task.CompletedTask;
        }, dependsOn: "level2-step1");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        Assert.Equal(5, executionOrder.Count);

        var level1Steps = executionOrder.Where(x => x.step.StartsWith("level1-")).ToList();
        var level2Steps = executionOrder.Where(x => x.step.StartsWith("level2-")).ToList();
        var level3Steps = executionOrder.Where(x => x.step.StartsWith("level3-")).ToList();

        Assert.True(level1Steps.All(l1 => level2Steps.All(l2 => l1.time <= l2.time)),
            "All level 1 steps should start before or at same time as level 2 steps");
        Assert.True(level2Steps.All(l2 => level3Steps.All(l3 => l2.time <= l3.time)),
            "All level 2 steps should start before or at same time as level 3 steps");
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineStepAnnotation_ExecutesAnnotatedSteps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var executedSteps = new List<string>();
        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithAnnotation(new PipelineStepAnnotation(() => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) =>
                {
                    executedSteps.Add("annotated-step");
                    await Task.CompletedTask;
                }
            }));

        var pipeline = new DistributedApplicationPipeline();
        pipeline.AddStep("regular-step", async (context) =>
        {
            executedSteps.Add("regular-step");
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
            .WithAnnotation(new PipelineStepAnnotation(() => new[]
            {
                new PipelineStep
                {
                    Name = "annotated-step-1",
                    Action = async (ctx) =>
                    {
                        executedSteps.Add("annotated-step-1");
                        await Task.CompletedTask;
                    }
                },
                new PipelineStep
                {
                    Name = "annotated-step-2",
                    Action = async (ctx) =>
                    {
                        executedSteps.Add("annotated-step-2");
                        await Task.CompletedTask;
                    }
                }
            }));

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
            executedSteps.Add("a");
            await Task.CompletedTask;
        });

        pipeline.AddStep("b", async (context) =>
        {
            executedSteps.Add("b");
            await Task.CompletedTask;
        }, dependsOn: "a");

        pipeline.AddStep("c", async (context) =>
        {
            executedSteps.Add("c");
            await Task.CompletedTask;
        }, dependsOn: "a");

        pipeline.AddStep("d", async (context) =>
        {
            executedSteps.Add("d");
            await Task.CompletedTask;
        }, dependsOn: "b", requiredBy: "e");

        pipeline.AddStep("e", async (context) =>
        {
            executedSteps.Add("e");
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
            executedSteps.Add("step1");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step2", async (context) =>
        {
            executedSteps.Add("step2");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
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
            executedSteps.Add("step1");
            await Task.CompletedTask;
        }, requiredBy: new[] { "step2", "step3" });

        pipeline.AddStep("step2", async (context) =>
        {
            executedSteps.Add("step2");
            await Task.CompletedTask;
        });

        pipeline.AddStep("step3", async (context) =>
        {
            executedSteps.Add("step3");
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

    private static DeployingContext CreateDeployingContext(DistributedApplication app)
    {
        return new DeployingContext(
            app.Services.GetRequiredService<DistributedApplicationModel>(),
            app.Services.GetRequiredService<DistributedApplicationExecutionContext>(),
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: null);
    }

    private sealed class CustomResource(string name) : Resource(name)
    {
    }
}
