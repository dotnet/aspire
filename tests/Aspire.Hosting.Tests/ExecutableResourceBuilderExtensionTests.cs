// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class ExecutableResourceBuilderExtensionTests
{
    [Theory]
    [InlineData("/absolute")]
    [InlineData("relative")]
    public void AddExecutableNormalisesWorkingDirectory(string workingDirectory)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", workingDirectory);

        var expectedPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));
        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal(expectedPath, annotation.WorkingDirectory);
    }

    [Fact]
    public void WithCommandMutatesCommand()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "workingdirectory");

        executable.WithCommand("newcommand");
        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal("newcommand", annotation.Command);
    }

    [Theory]
    [InlineData("/absolute")]
    [InlineData("relative")]
    public void WithWorkingDirectoryMutatesAndNormalisesWorkingDirectory(string workingDirectory)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("myexe", "command", "/whatever/workingdirectory");

        executable.WithWorkingDirectory(workingDirectory);

        var expectedPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));
        var annotation = executable.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
        Assert.Equal(expectedPath, annotation.WorkingDirectory);
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
        Assert.Equal(builder.AppHostDirectory, annotation.WorkingDirectory);
    }
}