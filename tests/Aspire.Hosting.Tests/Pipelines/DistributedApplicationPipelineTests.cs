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
                    lock (executedSteps) { executedSteps.Add("annotated-step"); }
                    await Task.CompletedTask;
                }
            }));

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
            .WithAnnotation(new PipelineStepAnnotation(() => new[]
            {
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
            .WithAnnotation(new PipelineStepAnnotation(() => new PipelineStep
            {
                Name = "duplicate-step",
                Action = async (ctx) => await Task.CompletedTask
            }));

        var resource2 = builder.AddResource(new CustomResource("resource2"))
            .WithAnnotation(new PipelineStepAnnotation(() => new PipelineStep
            {
                Name = "duplicate-step",
                Action = async (ctx) => await Task.CompletedTask
            }));

        var pipeline = new DistributedApplicationPipeline();
        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Duplicate step name", exception.Message);
        Assert.Contains("duplicate-step", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleStepsFailingAtSameLevel_ThrowsAggregateException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("failing-step1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error from step 1");
        });

        pipeline.AddStep("failing-step2", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error from step 2");
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        Assert.Contains("Multiple pipeline steps failed", exception.Message);
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step1"));
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step2"));
    }

    [Fact]
    public async Task ExecuteAsync_WithMixOfSuccessfulAndFailingStepsAtSameLevel_ThrowsAggregateException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var successfulStepExecuted = false;

        pipeline.AddStep("successful-step", async (context) =>
        {
            successfulStepExecuted = true;
            await Task.CompletedTask;
        });

        pipeline.AddStep("failing-step1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error from step 1");
        });

        pipeline.AddStep("failing-step2", async (context) =>
        {
            await Task.CompletedTask;
            throw new NotSupportedException("Error from step 2");
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        Assert.True(successfulStepExecuted, "Successful step should have executed");
        Assert.Contains("Multiple pipeline steps failed", exception.Message);
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step1"));
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step2"));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleFailuresAtSameLevel_StopsExecutionOfNextLevel()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var nextLevelStepExecuted = false;

        pipeline.AddStep("failing-step1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error from step 1");
        });

        pipeline.AddStep("failing-step2", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error from step 2");
        });

        pipeline.AddStep("next-level-step", async (context) =>
        {
            nextLevelStepExecuted = true;
            await Task.CompletedTask;
        }, dependsOn: "failing-step1");

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        Assert.False(nextLevelStepExecuted, "Next level step should not have executed");
        Assert.Equal(2, exception.InnerExceptions.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithThreeStepsFailingAtSameLevel_CapturesAllExceptions()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("failing-step1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error 1");
        });

        pipeline.AddStep("failing-step2", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error 2");
        });

        pipeline.AddStep("failing-step3", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error 3");
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        Assert.Equal(3, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step1"));
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step2"));
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("failing-step3"));
    }

    [Fact]
    public async Task ExecuteAsync_WithDifferentExceptionTypesAtSameLevel_CapturesAllTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("invalid-op-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Invalid operation");
        });

        pipeline.AddStep("not-supported-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new NotSupportedException("Not supported");
        });

        pipeline.AddStep("argument-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new ArgumentException("Bad argument");
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        Assert.Equal(3, exception.InnerExceptions.Count);

        var innerExceptions = exception.InnerExceptions.ToList();
        Assert.Contains(innerExceptions, e => e is InvalidOperationException && e.Message.Contains("invalid-op-step"));
        Assert.Contains(innerExceptions, e => e is InvalidOperationException && e.Message.Contains("not-supported-step"));
        Assert.Contains(innerExceptions, e => e is InvalidOperationException && e.Message.Contains("argument-step"));
    }

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
    public async Task ExecuteAsync_WithParallelSuccessfulAndFailingSteps_OnlyFailuresReported()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var executedSteps = new List<string>();

        pipeline.AddStep("success1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("success1"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("fail1", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("fail1"); }
            await Task.CompletedTask;
            throw new InvalidOperationException("Failure 1");
        });

        pipeline.AddStep("success2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("success2"); }
            await Task.CompletedTask;
        });

        pipeline.AddStep("fail2", async (context) =>
        {
            lock (executedSteps) { executedSteps.Add("fail2"); }
            await Task.CompletedTask;
            throw new InvalidOperationException("Failure 2");
        });

        var context = CreateDeployingContext(builder.Build());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));

        // All steps should have attempted to execute
        Assert.Contains("success1", executedSteps);
        Assert.Contains("success2", executedSteps);
        Assert.Contains("fail1", executedSteps);
        Assert.Contains("fail2", executedSteps);

        // Only failures should be in the exception
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, e => Assert.IsType<InvalidOperationException>(e));
    }

    [Fact]
    public async Task PublishAsync_Deploy_WithNoResourcesAndNoPipelineSteps_ReturnsError()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);

        var interactionService = PublishingActivityReporterTests.CreateInteractionService();
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

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
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

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
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithAnnotation(new PipelineStepAnnotation(() => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) => await Task.CompletedTask
            }));

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
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

        var resource = builder.AddResource(new CustomResource("test-resource"))
            .WithAnnotation(new PipelineStepAnnotation(() => new PipelineStep
            {
                Name = "annotated-step",
                Action = async (ctx) => await Task.CompletedTask
            }));

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
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

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
        var reporter = new PublishingActivityReporter(interactionService, NullLogger<PublishingActivityReporter>.Instance);

        builder.Services.AddSingleton<IPublishingActivityReporter>(reporter);

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
    public async Task ExecuteAsync_WithMultipleDependencyFailures_ReportsAllFailedDependencies()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var dependentStepExecuted = false;

        // Two steps that will fail
        pipeline.AddStep("failing-dep1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Dependency 1 failed");
        });

        pipeline.AddStep("failing-dep2", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Dependency 2 failed");
        });

        // Step that depends on both failing steps
        pipeline.AddStep("dependent-step", async (context) =>
        {
            dependentStepExecuted = true;
            await Task.CompletedTask;
        }, dependsOn: new[] { "failing-dep1", "failing-dep2" });

        var context = CreateDeployingContext(builder.Build());

        var ex = await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));
        
        // The dependent step should not have executed
        Assert.False(dependentStepExecuted, "Dependent step should not execute when dependencies fail");
        
        // Should report multiple failures
        Assert.Contains("Multiple pipeline steps failed", ex.Message);
        Assert.Equal(2, ex.InnerExceptions.Count);
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
    public async Task ExecuteAsync_WithFailingStep_TracksFailedStep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var expectedException = new InvalidOperationException("Test failure");
        pipeline.AddStep("failing-step", async (context) =>
        {
            await Task.CompletedTask;
            throw expectedException;
        });

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // Verify that the failed step was tracked
        Assert.Single(pipeline.FailedSteps);
        Assert.Equal("failing-step", pipeline.FailedSteps[0].StepName);
        Assert.NotNull(pipeline.FailedSteps[0].Exception);
        Assert.Contains("failing-step", pipeline.FailedSteps[0].Exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleFailingSteps_TracksAllFailures()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        pipeline.AddStep("failing-step1", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Error 1");
        });

        pipeline.AddStep("failing-step2", async (context) =>
        {
            await Task.CompletedTask;
            throw new NotSupportedException("Error 2");
        });

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));

        // Verify that both failed steps were tracked
        Assert.Equal(2, pipeline.FailedSteps.Count);
        
        var failedStepNames = pipeline.FailedSteps.Select(f => f.StepName).OrderBy(n => n).ToList();
        Assert.Equal(["failing-step1", "failing-step2"], failedStepNames);
        
        // Verify exceptions are tracked
        Assert.All(pipeline.FailedSteps, f => Assert.NotNull(f.Exception));
    }

    [Fact]
    public async Task ExecuteAsync_WithDependencyFailure_TracksOnlyDirectFailures()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var dependentStepExecuted = false;

        pipeline.AddStep("failing-dependency", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Dependency failed");
        });

        pipeline.AddStep("dependent-step", async (context) =>
        {
            dependentStepExecuted = true;
            await Task.CompletedTask;
        }, dependsOn: "failing-dependency");

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));

        // Only the step that actually threw an exception should be tracked
        Assert.Single(pipeline.FailedSteps);
        Assert.Equal("failing-dependency", pipeline.FailedSteps[0].StepName);
        
        // The dependent step should not have executed
        Assert.False(dependentStepExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_ClearsFailuresFromPreviousExecution()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        // First execution - add a failing step
        pipeline.AddStep("failing-step", async (context) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test failure");
        });

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context));
        Assert.Single(pipeline.FailedSteps);

        // Second execution - create a new pipeline with successful step
        var pipeline2 = new DistributedApplicationPipeline();
        pipeline2.AddStep("successful-step", async (context) =>
        {
            await Task.CompletedTask;
        });

        await pipeline2.ExecuteAsync(context);

        // Failed steps should be empty after successful execution
        Assert.Empty(pipeline2.FailedSteps);
    }

    [Fact]
    public async Task ExecuteAsync_FailedStepsCanBeUsedForDetailedErrorReporting()
    {
        // This test demonstrates how the CLI or Publisher can use FailedSteps for error reporting
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var exception1 = new InvalidOperationException("Database connection failed");
        var exception2 = new TimeoutException("Deployment timeout");

        pipeline.AddStep("deploy-database", async (context) =>
        {
            await Task.CompletedTask;
            throw exception1;
        });

        pipeline.AddStep("deploy-service", async (context) =>
        {
            await Task.CompletedTask;
            throw exception2;
        });

        var context = CreateDeployingContext(builder.Build());

        await Assert.ThrowsAsync<AggregateException>(() => pipeline.ExecuteAsync(context));

        // Verify we can access detailed failure information for reporting
        Assert.Equal(2, pipeline.FailedSteps.Count);

        // Simulate building an error report like the CLI would
        var errorReport = string.Join(", ", pipeline.FailedSteps.Select(f => $"{f.StepName}: {f.Exception.Message}"));
        Assert.Contains("deploy-database", errorReport);
        Assert.Contains("Database connection failed", errorReport);
        Assert.Contains("deploy-service", errorReport);
        Assert.Contains("Deployment timeout", errorReport);

        // Verify exception types are preserved
        var dbFailure = pipeline.FailedSteps.First(f => f.StepName == "deploy-database");
        Assert.IsType<InvalidOperationException>(dbFailure.Exception);
        Assert.Contains("Database connection failed", dbFailure.Exception.Message);

        var serviceFailure = pipeline.FailedSteps.First(f => f.StepName == "deploy-service");
        Assert.IsType<InvalidOperationException>(serviceFailure.Exception);
        Assert.Contains("Deployment timeout", serviceFailure.Exception.Message);
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

    [Fact]
    public async Task ExecuteAsync_WithLongAndShortBranches_DoesNotBlockShortBranch()
    {
        // Test that a long-running branch doesn't block an independent short branch
        // Pattern: A -> LongB, A -> ShortB -> C
        // C should be able to complete while LongB is still running
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var pipeline = new DistributedApplicationPipeline();

        var completionOrder = new List<string>();
        var completionTimes = new Dictionary<string, DateTime>();

        pipeline.AddStep("A", async (context) =>
        {
            await Task.Delay(10);
        });

        pipeline.AddStep("LongB", async (context) =>
        {
            await Task.Delay(100);
            lock (completionOrder)
            {
                completionOrder.Add("LongB");
                completionTimes["LongB"] = DateTime.UtcNow;
            }
        }, dependsOn: "A");

        pipeline.AddStep("ShortB", async (context) =>
        {
            await Task.Delay(10);
            lock (completionOrder)
            {
                completionOrder.Add("ShortB");
                completionTimes["ShortB"] = DateTime.UtcNow;
            }
        }, dependsOn: "A");

        pipeline.AddStep("C", async (context) =>
        {
            await Task.Delay(10);
            lock (completionOrder)
            {
                completionOrder.Add("C");
                completionTimes["C"] = DateTime.UtcNow;
            }
        }, dependsOn: "ShortB");

        var context = CreateDeployingContext(builder.Build());
        await pipeline.ExecuteAsync(context);

        // C should complete before LongB (demonstrating improved concurrency)
        var cIndex = completionOrder.IndexOf("C");
        var longBIndex = completionOrder.IndexOf("LongB");

        Assert.True(cIndex < longBIndex,
            "C should complete before LongB (not blocked by long-running parallel branch)");
        Assert.True(completionTimes["C"] < completionTimes["LongB"],
            "C should complete before LongB based on timestamps");
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
