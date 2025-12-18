// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerImageAnnotationTests
{
    [Fact]
    public void SettingTagNullsSha()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            SHA256 = "pretendthisisasha"
        };

        Assert.Null(annotation.Tag);
        annotation.Tag = "latest";
        Assert.Equal("latest", annotation.Tag);
        Assert.Null(annotation.SHA256);
    }

    [Fact]
    public void SettingShaNullsTag()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            Tag = "latest"
        };

        Assert.Null(annotation.SHA256);
        annotation.SHA256 = "pretendthisisasha";
        Assert.Equal("pretendthisisasha", annotation.SHA256);
        Assert.Null(annotation.Tag);
    }

    [Fact]
    public void PlatformCanBeSetIndependently()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            Tag = "latest",
            Platform = "linux/amd64"
        };

        Assert.Equal("latest", annotation.Tag);
        Assert.Equal("linux/amd64", annotation.Platform);
    }

    [Fact]
    public void PlatformDoesNotAffectTagOrSha()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            SHA256 = "pretendthisisasha",
            Platform = "linux/arm64"
        };

        Assert.Equal("pretendthisisasha", annotation.SHA256);
        Assert.Null(annotation.Tag);
        Assert.Equal("linux/arm64", annotation.Platform);
    }

}
