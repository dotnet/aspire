// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class WithServiceBindingTests
{
    [Fact]
    public void ServiceBindingsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithServiceBinding(3000, 1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithServiceBinding(3000, 2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Service binding with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void ServiceBindingsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithServiceBinding(1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithServiceBinding(2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Service binding with name 'mybinding' already exists", ex.Message);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithServiceBindingTests>(args);

}
