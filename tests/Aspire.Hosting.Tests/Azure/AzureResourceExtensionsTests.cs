// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureResourceExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedConnectionString(int? port = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(port);

        var connectionString = cosmos.Resource.GetConnectionString();
        Assert.NotNull(connectionString);

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", parts[0]);
        Assert.Equal($"AccountEndpoint=https://127.0.0.1:{port ?? 8081}", parts[1]);
    }
}
