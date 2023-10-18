// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class DitributedApplicationBuilderTests
{
    [Fact]
    public void AddingTwoResourcesWithSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = new TestProgram([]);
            testProgram.AppBuilder.AddRedisContainer("x");
            testProgram.AppBuilder.AddPostgresContainer("x");
        });

        Assert.Equal("Cannot add resource of type 'Aspire.Hosting.Postgres.PostgresContainerResource' with name 'x' because resource of type 'Aspire.Hosting.Redis.RedisContainerResource' with that name already exists.", ex.Message);
    }
}
