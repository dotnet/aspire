// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using static Aspire.Hosting.VolumeNameGenerator;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Utils;

public class VolumeNameGeneratorTests
{
    [Fact]
    public void VolumeGeneratorUsesUniqueName()
    {
        var builder = DistributedApplication.CreateBuilder();

        var volumePrefix = $"{Sanitize(builder.Environment.ApplicationName).ToLowerInvariant()}-{builder.Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";

        var resource = builder.AddResource(new TestResource("myresource"));

        var volumeName = Generate(resource, "data");

        Assert.Equal($"{volumePrefix}-{resource.Resource.Name}-data", volumeName);
    }

    [Theory]
    [MemberData(nameof(InvalidNameParts))]
    public void ThrowsWhenSuffixContainsInvalidChars(string suffix)
    {
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder.AddResource(new TestResource("myresource"));

        Assert.Throws<ArgumentException>(nameof(suffix), () => Generate(resource, suffix));
    }

    public static object[][] InvalidNameParts => [
        ["This/is/invalid"],
        [@"This\is\invalid"],
        ["_ThisIsInvalidToo"],
        [".ThisIsInvalidToo"],
        ["-ThisIsInvalidToo"],
        ["This&IsInvalidToo"]
    ];

    private sealed class TestResource(string name) : IResource
    {
        public string Name { get; } = name;

        public ResourceAnnotationCollection Annotations { get; } = [];
    }

    [Fact]
    public void VolumeNameDiffersBetweenPublishAndRun()
    {
        var runBuilder = TestDistributedApplicationBuilder.Create();
        var publishBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var runVolumePrefix = $"{Sanitize(runBuilder.Environment.ApplicationName).ToLowerInvariant()}-{runBuilder.Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";
        var publishVolumePrefix = $"{Sanitize(publishBuilder.Environment.ApplicationName).ToLowerInvariant()}-{publishBuilder.Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";

        var runResource = runBuilder.AddResource(new TestResource("myresource"));
        var publishResource = publishBuilder.AddResource(new TestResource("myresource"));

        var runVolumeName = Generate(runResource, "data");
        var publishVolumeName = Generate(publishResource, "data");

        Assert.Equal($"{runVolumePrefix}-{runResource.Resource.Name}-data", runVolumeName);
        Assert.Equal($"{publishVolumePrefix}-{publishResource.Resource.Name}-data", publishVolumeName);
        Assert.NotEqual(runVolumeName, publishVolumeName);
    }

    [Theory]
    [InlineData(@"C:\Project\App")]
    [InlineData(@"c:\project\app")]
    [InlineData(@"C:/Project/App")]
    [InlineData(@"C:\Project\App\")]
    [InlineData(@"C:\Project\..\Project\App")]
    public void VolumeNameConsistentAcrossPathCasingsAndFormats(string projectDirectory)
    {
        // This test verifies that different representations of the same path produce the same volume name
        // when using DistributedApplicationBuilder with DistributedApplicationOptions.ProjectDirectory
        
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = projectDirectory,
            Args = [] // Ensure run mode (default)
        };
        
        var builder = DistributedApplication.CreateBuilder(options);
        
        // Verify this is in run mode so path-based hashing is used
        Assert.False(builder.ExecutionContext.IsPublishMode);
        Assert.True(builder.ExecutionContext.IsRunMode);
        
        var appHostSha = builder.Configuration["AppHost:Sha256"];
        Assert.NotNull(appHostSha);
        
        // Create a resource and verify volume name generation works
        var resource = builder.AddResource(new TestResource("myresource"));
        var volumeName = Generate(resource, "data");
        Assert.NotNull(volumeName);
        Assert.Contains("myresource-data", volumeName);
        
        // On Windows, all these different path representations should produce the same SHA
        // because the path normalization logic applies Path.GetFullPath() and ToLowerInvariant()
        // On non-Windows systems, the paths are used as-is but still normalized with Path.GetFullPath()
        if (OperatingSystem.IsWindows())
        {
            // On Windows, all test paths should normalize to the same value and produce the same SHA
            // Compute the expected SHA directly using the same logic as the builder
            var referencePath = Path.Join(@"c:\project\app", builder.Environment.ApplicationName);
            var normalizedReferencePath = Path.GetFullPath(referencePath).ToLowerInvariant();
            var expectedShaBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedReferencePath));
            var expectedSha = Convert.ToHexString(expectedShaBytes);
            
            // All test paths should produce the same SHA as the reference due to case-insensitive normalization
            Assert.Equal(expectedSha, appHostSha);
        }
        else
        {
            // On non-Windows systems, verify that the path normalization still works with Path.GetFullPath()
            // Case differences will produce different SHAs, but path format normalization should still apply
            var normalizedProjectDir = Path.GetFullPath(projectDirectory);
            var normalizedPath = Path.Join(normalizedProjectDir, builder.Environment.ApplicationName);
            var normalizedAppHostPath = Path.GetFullPath(normalizedPath);
            var expectedShaBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedAppHostPath));
            var expectedSha = Convert.ToHexString(expectedShaBytes);
            
            // The normalized path should produce the same SHA as the original path after normalization
            Assert.Equal(expectedSha, appHostSha);
        }
    }
}
