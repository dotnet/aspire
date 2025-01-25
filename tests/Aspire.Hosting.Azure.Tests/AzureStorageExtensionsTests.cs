// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotationWithDefaultPath(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal(Path.Combine(builder.AppHostDirectory, ".azurite", "storage"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotationWithDefaultName(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal($"{builder.GetVolumePrefix()}-storage-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.Equal("mydata", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureStorageUserEmulatorUseBlobQueueTablePortMethodsMutateEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithBlobPort(9001);
            builder.WithQueuePort(9002);
            builder.WithTablePort(9003);
        });

        Assert.Collection(
            storage.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(9001, e.Port),
            e => Assert.Equal(9002, e.Port),
            e => Assert.Equal(9003, e.Port));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureStorage_WithApiVersionCheck_ShouldSetSkipApiVersionCheck(bool enableApiVersionCheck)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(x => x.WithApiVersionCheck(enableApiVersionCheck));

        Assert.True(storage.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.All(["azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0"], x => args.Contains(x));

        if (enableApiVersionCheck)
        {
            Assert.Contains("--skipApiVersionCheck", args);
        }
        else
        {
            Assert.DoesNotContain("--skipApiVersionCheck", args);
        }
    }

    [Fact]
    public void AddAzureStorage_RunAsEmulator_SetSkipApiVersionCheck()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();

        Assert.True(storage.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Contains("--skipApiVersionCheck", args);
    }
}
