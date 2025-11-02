// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECONTAINERRUNTIME001

namespace Aspire.Hosting.Tests.Publishing;

internal sealed class FakeContainerRuntime(bool shouldFail = false) : IContainerRuntime
{
    public string Name => "fake-runtime";
    public bool WasHealthCheckCalled { get; private set; }
    public bool WasTagImageCalled { get; private set; }
    public bool WasRemoveImageCalled { get; private set; }
    public bool WasPushImageCalled { get; private set; }
    public bool WasBuildImageCalled { get; private set; }
    public bool WasLoginToRegistryCalled { get; private set; }
    public List<(string localImageName, string targetImageName)> TagImageCalls { get; } = [];
    public List<string> RemoveImageCalls { get; } = [];
    public List<string> PushImageCalls { get; } = [];
    public List<(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options)> BuildImageCalls { get; } = [];
    public List<(string registryServer, string username, string password)> LoginToRegistryCalls { get; } = [];
    public Dictionary<string, string?>? CapturedBuildArguments { get; private set; }
    public Dictionary<string, string?>? CapturedBuildSecrets { get; private set; }
    public string? CapturedStage { get; private set; }

    public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken)
    {
        WasHealthCheckCalled = true;
        return Task.FromResult(!shouldFail);
    }

    public Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken)
    {
        WasTagImageCalled = true;
        TagImageCalls.Add((localImageName, targetImageName));
        if (shouldFail)
        {
            throw new InvalidOperationException("Fake container runtime is configured to fail");
        }
        return Task.CompletedTask;
    }

    public Task RemoveImageAsync(string imageName, CancellationToken cancellationToken)
    {
        WasRemoveImageCalled = true;
        RemoveImageCalls.Add(imageName);
        if (shouldFail)
        {
            throw new InvalidOperationException("Fake container runtime is configured to fail");
        }
        return Task.CompletedTask;
    }

    public Task PushImageAsync(string imageName, CancellationToken cancellationToken)
    {
        WasPushImageCalled = true;
        PushImageCalls.Add(imageName);
        if (shouldFail)
        {
            throw new InvalidOperationException("Fake container runtime is configured to fail");
        }
        return Task.CompletedTask;
    }

    public Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken)
    {
        // Capture the arguments for verification in tests
        CapturedBuildArguments = buildArguments;
        CapturedBuildSecrets = buildSecrets;
        CapturedStage = stage;
        WasBuildImageCalled = true;
        BuildImageCalls.Add((contextPath, dockerfilePath, imageName, options));

        if (shouldFail)
        {
            throw new InvalidOperationException("Fake container runtime is configured to fail");
        }

        // For testing, we don't need to actually build anything
        return Task.CompletedTask;
    }

    public Task LoginToRegistryAsync(string registryServer, string username, string password, CancellationToken cancellationToken)
    {
        WasLoginToRegistryCalled = true;
        LoginToRegistryCalls.Add((registryServer, username, password));
        if (shouldFail)
        {
            throw new InvalidOperationException("Fake container runtime is configured to fail");
        }
        return Task.CompletedTask;
    }
}
