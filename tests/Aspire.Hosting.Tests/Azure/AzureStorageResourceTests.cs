// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureStorageResourceTests
{
    [Fact]
    public void AzureStorageReferenceGetsExpectedConnectionString()
    {
        var testProgram = CreateTestProgram();

        var storage = testProgram.AppBuilder.AddAzureStorage("storage").UseEmulator();

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("blob",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "http"
            ));

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("queue",
            ProtocolType.Tcp,
            "localhost",
            3001,
            "http"
            ));

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("table",
            ProtocolType.Tcp,
            "localhost",
            3002,
            "http"
            ));

        testProgram.ServiceABuilder.WithReference(storage);

        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        // Simple test to ensure that the connection string exists and appears to contain all three endpoints.
        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(
            config, 
            kvp => kvp.Key == "ConnectionStrings__storage" 
                && Regex.IsMatch(kvp.Value, "BlobEndpoint=[^;]+;")
                && Regex.IsMatch(kvp.Value, "QueueEndpoint=[^;]+;")
                && Regex.IsMatch(kvp.Value, "TableEndpoint=[^;]+;"));
    }

    [Fact]
    public void AzureStorageConnectionStringFailsWhenNotEmulated()
    {
        var testProgram = CreateTestProgram();

        var storage = testProgram.AppBuilder.AddAzureStorage("storage");

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("blob",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "http"
            ));

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("queue",
            ProtocolType.Tcp,
            "localhost",
            3001,
            "http"
            ));

        storage.WithAnnotation(
            new AllocatedEndpointAnnotation("table",
            ProtocolType.Tcp,
            "localhost",
            3002,
            "http"
            ));

        testProgram.ServiceABuilder.WithReference(storage);

        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        Assert.Throws<DistributedApplicationException>(() =>
        {
            foreach (var annotation in annotations)
            {
                annotation.Callback(context);
            }
        });
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);
}