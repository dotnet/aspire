// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.Hosting.Utils.VolumeNameGenerator;
using Xunit;
using System.Text;
using System.Security.Cryptography;

namespace Aspire.Hosting.Tests.Utils;

public class VolumeNameGeneratorTests
{
    [Fact]
    public void VolumeGeneratorUsesFirst10CharsOfSha256FromAppHostConfig()
    {
        var builder = DistributedApplication.CreateBuilder();
        var appHostSha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("app")));
        builder.Configuration["AppHost:Sha256"] = appHostSha;
        var resource = builder.AddResource(new TestResource("myresource"));

        var volumeName = CreateVolumeName(resource, "data");

        Assert.Equal($"{appHostSha[..10].ToLowerInvariant()}-{resource.Resource.Name}-data", volumeName);
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
