// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerResourceBuilderTests
{
    [Fact]
    public void WithCommandMutatesCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "workingdirectory");

        executable.WithCommand("newcommand");
        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal("newcommand", annotation.Command);
    }

    [Fact]
    public void WithWorkingDirectoryMutatesWorkingDirectory()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "workingdirectory");

        executable.WithWorkingDirectory("newworkingdirectory");
        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal("newworkingdirectory", annotation.WorkingDirectory);
    }

    [Fact]
    public void WithCommandDoesNotAllowEmptyString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "workingdirectory");

        Assert.Throws<ArgumentException>(() => executable.WithCommand(""));
    }

    [Fact]
    public void WithWorkingDirectoryAllowsEmptyString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "workingdirectory");

        executable.WithWorkingDirectory("");

        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal("newworkingdirectory", annotation.WorkingDirectory);

    }    
}