// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Native.Api;
using Aspire.Hosting.Native.Core.Tests.TestServices;
using Aspire.Hosting.Native.Model;
using Aspire.Hosting.Native.Runtime;

namespace Aspire.Hosting.Native.Core.Tests;

public class DeclarativeApplicationTests
{
    [Fact]
    public async Task WorkspaceRejectsRevisionWhileDeclaredStartupStillOwnsItsDesiredPlan()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = new WorkloadTestExecutor
        {
            BeforeStart = token =>
            {
                entered.TrySetResult();
                return release.Task.WaitAsync(token);
            }
        };
        executor.ReleaseCleanup();
        var workspace = new NativeApplicationServer(executor).CreateApplicationWorkspace();
        var first = workspace.BeginRevision();
        first.AddContainer("service", "generic/service:1");
        await workspace.CommitRevision(first);
        var application = first.StartApplication();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15), TimeProvider.System);
        var next = workspace.BeginRevision();
        next.AddContainer("service", "generic/service:2");
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => workspace.CommitRevision(next));
        Assert.Equal("The declared application is still activating.", failure.Message);
        workspace.AbortRevision(next);
        release.TrySetResult();
        await application.WaitApplicationReady(15000);
        Assert.False(first.IsRetired);
        await workspace.RetireApplicationWorkspace();
    }

    [Fact]
    public async Task DeclaredRetirementStopsConsumersBeforeProducersRegardlessOfNameOrder()
    {
        var executor = new WorkloadTestExecutor();
        executor.ReleaseCleanup();
        var session = new NativeApplicationServer(executor).OpenApplication();
        var composition = session.StartGeneration();
        var producer = composition.AddContainer("a-producer", "generic/service:1");
        var consumer = composition.AddContainer("z-consumer", "generic/consumer:1");
        consumer.WaitFor(producer);
        await composition.StartApplication().WaitApplicationReady(15000);
        await session.RetireGeneration(composition);
        Assert.Equal([consumer.Handle.ResourceId, producer.Handle.ResourceId], executor.Removed.Select(identity => identity.ResourceId));
    }

    [Fact]
    public async Task InitializationFailureRunsDeclaredCleanupEvenWhenMainWorkloadNeverStarted()
    {
        var executor = new WorkloadTestExecutor
        {
            StatusForPlan = plan => new(WorkloadPhase.Exited, plan.Arguments.SequenceEqual(["initialize"]) ? 1 : 0)
        };
        executor.ReleaseCleanup();
        var session = new NativeApplicationServer(executor).OpenApplication();
        var composition = session.StartGeneration();
        var resource = composition.AddContainer("service", "generic/service:1");
        foreach (var phase in new[] { "initialize", "retire" })
        {
            resource.WithLifecycleExecutable(phase, new()
            {
                ExecutablePath = Environment.ProcessPath!,
                WorkingDirectory = AppContext.BaseDirectory,
                Arguments = [phase],
                Environment = [],
                Endpoints = []
            }, new() { Environment = [], Arguments = [] });
        }
        var application = composition.StartApplication();
        await Assert.ThrowsAsync<InvalidOperationException>(() => application.WaitApplicationReady(15000));
        await session.RetireGeneration(composition);
        Assert.Equal(["initialize", "retire"], executor.Plans.Select(plan => Assert.Single(plan.Arguments)));
        Assert.Equal(2, executor.Removed.Count);
    }

    [Fact]
    public async Task DeclaredWorkspaceRevisionsRetainUnchangedWorkloadsAndRestartAffectedConsumers()
    {
        var executor = new WorkloadTestExecutor();
        executor.ReleaseCleanup();
        var workspace = new NativeApplicationServer(executor).CreateApplicationWorkspace();
        Composition Declare(string image)
        {
            var revision = workspace.BeginRevision();
            var producer = revision.AddContainer("producer", image);
            producer.WithEndpoint(new() { Name = "tcp", Protocol = "TCP", UriScheme = "tcp", TargetPort = 6379, HostPort = 0, EnvironmentVariable = "" });
            var consumer = revision.AddExecutable("consumer", Environment.ProcessPath!, AppContext.BaseDirectory);
            consumer.WithReference("HOST", revision.CreateEndpointValue(producer, "tcp", "host"));
            return revision;
        }
        var first = Declare("generic/service:1");
        await workspace.CommitRevision(first);
        await first.StartApplication().WaitApplicationReady(15000);
        Assert.Equal(2, executor.Started.Count);
        await workspace.CommitRevision(Declare("generic/service:1"));
        Assert.Equal(2, executor.Started.Count);
        await workspace.CommitRevision(Declare("generic/service:2"));
        Assert.Equal(4, executor.Started.Count);
        Assert.Equal(2, executor.Removed.Count);
        Assert.True(first.IsRetired);
        await workspace.RetireApplicationWorkspace();
        Assert.Equal(4, executor.Removed.Count);
    }

    [Fact]
    public async Task FailedProtocolHealthUsesInjectedClockAndCleansContainerWithoutStartingDependents()
    {
        var probeWaiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = new AdjustableFakeTimeProvider
        {
            TimerCreated = due => { if (due == TimeSpan.FromSeconds(1)) { probeWaiting.TrySetResult(); } }
        };
        var executor = new WorkloadTestExecutor
        {
            StatusForPlan = plan => new(plan is ContainerWorkload ? WorkloadPhase.Running : WorkloadPhase.Exited, 1)
        };
        executor.ReleaseCleanup();
        var options = new NativeRuntimeOptions
        {
            TimeProvider = clock,
            WorkloadStartupTimeout = TimeSpan.FromSeconds(10),
            ObservationInterval = TimeSpan.FromSeconds(1)
        };
        var session = new NativeApplicationServer(executor, options).OpenApplication();
        var composition = session.StartGeneration();
        var service = composition.AddContainer("service", "generic/service:1");
        service.WithLifecycleExecutable("health", new()
        {
            ExecutablePath = Environment.ProcessPath!,
            WorkingDirectory = AppContext.BaseDirectory,
            Arguments = ["reject-authentication"],
            Environment = [],
            Endpoints = []
        }, new() { Arguments = [], Environment = [] });
        var consumer = composition.AddExecutable("consumer", Environment.ProcessPath!, AppContext.BaseDirectory);
        consumer.WaitFor(service);
        var application = composition.StartApplication();
        var readiness = application.WaitApplicationReady(15000);
        await probeWaiting.Task.WaitAsync(TimeSpan.FromSeconds(15), TimeProvider.System);
        clock.Advance(TimeSpan.FromSeconds(10));
        var failure = await Record.ExceptionAsync(() => readiness);
        Assert.True(failure is OperationCanceledException or InvalidOperationException);
        await session.RetireGeneration(composition);
        Assert.All(executor.Plans.OfType<ExecutableWorkload>(), plan => Assert.Equal(["reject-authentication"], plan.Arguments));
        Assert.Equal(executor.Started.Count, executor.Removed.Count);
    }

    [Fact]
    public async Task NativeApplicationStartsDeclaredWorkloadsAndAuthenticatedLifecycleWithoutInvitations()
    {
        var executor = new WorkloadTestExecutor
        {
            StatusForPlan = plan => new(plan is ContainerWorkload ? WorkloadPhase.Running : WorkloadPhase.Exited, 0)
        };
        executor.ReleaseCleanup();
        var server = new NativeApplicationServer(executor);
        var session = server.OpenApplication();
        var composition = session.StartGeneration();
        var secret = composition.AddSecretParameter("password", "native-only-secret");
        var service = composition.AddContainer("service", "generic/service:1");
        foreach (var phase in new[] { "initialize", "ready" })
        {
            service.WithLifecycleExecutable(phase, new()
            {
                ExecutablePath = Environment.ProcessPath!,
                WorkingDirectory = AppContext.BaseDirectory,
                Environment = [],
                Arguments = [phase],
                Endpoints = []
            }, new() { Environment = [], Arguments = [] });
        }
        service.WithEnvironment("PASSWORD", secret);
        service.WithEndpoint(new() { Name = "tcp", Protocol = "TCP", UriScheme = "tcp", TargetPort = 6379, HostPort = 0, EnvironmentVariable = "" });
        service.SetSymbolicConfiguration("probe.host", composition.CreateEndpointValue(service, "tcp", "host"));
        service.SetSymbolicConfiguration("probe.password", secret);
        service.WithLifecycleExecutable("health", new()
        {
            ExecutablePath = Environment.ProcessPath!,
            WorkingDirectory = AppContext.BaseDirectory,
            Environment = [],
            Arguments = ["health"],
            Endpoints = []
        }, new()
        {
            Environment = [new() { Name = "HOST", ConfigurationKey = "probe.host" }, new() { Name = "PASSWORD", ConfigurationKey = "probe.password" }],
            Arguments = []
        });
        service.WithLifecycleExecutable("retire", new()
        {
            ExecutablePath = Environment.ProcessPath!,
            WorkingDirectory = AppContext.BaseDirectory,
            Environment = [],
            Arguments = ["retire"],
            Endpoints = []
        }, new() { Environment = [], Arguments = [] });
        var consumer = composition.AddExecutable("consumer", Environment.ProcessPath!, AppContext.BaseDirectory);
        consumer.WithReference("SERVICE_HOST", composition.CreateEndpointValue(service, "tcp", "host"));
        Assert.Empty(executor.Started);
        var application = composition.StartApplication();
        await application.WaitApplicationReady(15000);
        Assert.True(await application.WaitResourceCompletion(consumer, 15000));
        Assert.Equal(5, executor.Started.Count);
        var health = Assert.Single(executor.Plans.OfType<ExecutableWorkload>(), plan => plan.Arguments.SequenceEqual(["health"]));
        Assert.Collection(health.Environment, entry => Assert.Equal(new WorkloadEnvironment("HOST", "127.0.0.1"), entry),
            entry => Assert.Equal(new WorkloadEnvironment("PASSWORD", "native-only-secret"), entry));
        await session.RetireGeneration(composition);
        Assert.Equal(6, executor.Removed.Count);
        Assert.Single(executor.Plans.OfType<ExecutableWorkload>(), plan => plan.Arguments.SequenceEqual(["retire"]));
    }

    [Fact]
    public async Task ContainerReferenceRejectsUnsupportedNetworkContextInsteadOfUsingHostLoopback()
    {
        var executor = new WorkloadTestExecutor();
        executor.ReleaseCleanup();
        var session = new NativeApplicationServer(executor).OpenApplication();
        var composition = session.StartGeneration();
        var service = composition.AddContainer("service", "generic/service:1");
        var consumer = composition.AddContainer("consumer", "generic/consumer:1");
        consumer.WithReference("SERVICE", composition.CreateEndpointValue(service, "tcp", "host"));
        var application = composition.StartApplication();
        await Assert.ThrowsAsync<SymbolicValueException>(() => application.WaitApplicationReady(15000));
        await session.RetireGeneration(composition);
    }
}
