// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias AspireHostingShared;

using static AspireHostingShared::Aspire.Hosting.Utils.VolumeNameGenerator;
using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class VolumeNameGeneratorTests
{
    [Theory]
    [InlineData("ACustomButValidAppName")]
    [InlineData("0123ThisIsValidToo")]
    [InlineData("This_Is_Valid_Too")]
    [InlineData("This.Is.Valid.Too")]
    [InlineData("This-Is-Valid-Too")]
    [InlineData("This_Is.Valid-Too")]
    [InlineData("This_0Is.1Valid-2Too")]
    [InlineData("This_---.....---___Is_Valid_Too")]
    public void UsesApplicationNameAsPrefixIfCharsAreValid(string applicationName)
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Environment.ApplicationName = applicationName;
        var resource = builder.AddResource(new TestResource("myresource"));

        var volumeName = CreateVolumeName(resource, "data");

        Assert.Equal($"{builder.Environment.ApplicationName}-{resource.Resource.Name}-data", volumeName);
    }

    [Theory]
    [MemberData(nameof(InvalidNameParts))]
    public void UsesVolumeAsPrefixIfApplicationNameCharsAreInvalid(string applicationName)
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Environment.ApplicationName = applicationName;
        var resource = builder.AddResource(new TestResource("myresource"));

        var volumeName = CreateVolumeName(resource, "data");

        Assert.Equal($"volume-{resource.Resource.Name}-data", volumeName);
    }

    [Theory]
    [MemberData(nameof(InvalidNameParts))]
    public void ThrowsWhenSuffixContainsInvalidChars(string suffix)
    {
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder.AddResource(new TestResource("myresource"));

        Assert.Throws<ArgumentException>(nameof(suffix), () => CreateVolumeName(resource, suffix));
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
}
