// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.JavaScript.Tests;

public class AddJavaScriptAppTests
{
    [Fact]
    public async Task VerifyDockerfile()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        // Create directory to ensure manifest generates correct relative build context path
        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);

        var yarnApp = builder.AddJavaScriptApp("js", appDir)
            .WithYarn(installArgs: ["--immutable"])
            .WithBuildScript("do", ["--build"]);

        await ManifestUtils.GetManifest(yarnApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN yarn install --immutable
            RUN yarn run do --build

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);

        var dockerBuildAnnotation = yarnApp.Resource.Annotations.OfType<DockerfileBuildAnnotation>().Single();
        Assert.False(dockerBuildAnnotation.HasEntrypoint);

        var containerFilesSource = yarnApp.Resource.Annotations.OfType<ContainerFilesSourceAnnotation>().Single();
        Assert.Equal("/app/dist", containerFilesSource.SourcePath);
    }

    [Fact]
    public async Task VerifyPnpmDockerfile()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputPath: tempDir.Path).WithResourceCleanUp(true);

        // Create directory to ensure manifest generates correct relative build context path
        var appDir = Path.Combine(tempDir.Path, "js");
        Directory.CreateDirectory(appDir);

        var pnpmApp = builder.AddJavaScriptApp("js", appDir)
            .WithPnpm(installArgs: ["--prefer-frozen-lockfile"])
            .WithBuildScript("mybuild");

        await ManifestUtils.GetManifest(pnpmApp.Resource, tempDir.Path);

        var dockerfilePath = Path.Combine(tempDir.Path, "js.Dockerfile");
        var dockerfileContents = File.ReadAllText(dockerfilePath);
        var expectedDockerfile = $$"""
            FROM node:22-slim
            WORKDIR /app
            COPY . .
            RUN pnpm install --prefer-frozen-lockfile
            RUN pnpm run mybuild

            """.Replace("\r\n", "\n");
        Assert.Equal(expectedDockerfile, dockerfileContents);
    }

    [Fact]
    public void NodeAppWithDebugSupport_AddsAnnotationInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        var scriptPath = "app.js";
        File.WriteAllText(Path.Combine(tempDir.Path, scriptPath), "console.log('test');");

        var node = builder.AddNodeApp("mynode", tempDir.Path, scriptPath);

        var annotation = node.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
        Assert.Equal("node", annotation.LaunchConfigurationType);
    }

    [Fact]
    public void NodeAppWithDebugSupport_DoesNotAddAnnotationInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var tempDir = new TempDirectory();

        var scriptPath = "app.js";
        File.WriteAllText(Path.Combine(tempDir.Path, scriptPath), "console.log('test');");

        var node = builder.AddNodeApp("mynode", tempDir.Path, scriptPath);

        var annotation = node.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.Null(annotation);
    }

    [Fact]
    public void JavaScriptAppWithDebugSupport_AddsAnnotationInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), "{}");

        var jsApp = builder.AddJavaScriptApp("myjsapp", tempDir.Path, "dev");

        var annotation = jsApp.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
        Assert.Equal("node", annotation.LaunchConfigurationType);
    }

    [Fact]
    public void JavaScriptAppWithDebugSupport_DoesNotAddAnnotationInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var tempDir = new TempDirectory();

        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), "{}");

        var jsApp = builder.AddJavaScriptApp("myjsapp", tempDir.Path, "dev");

        var annotation = jsApp.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.Null(annotation);
    }

    [Fact]
    public void NodeAppWithDebuggerProperties_AddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        var scriptPath = "app.js";
        File.WriteAllText(Path.Combine(tempDir.Path, scriptPath), "console.log('test');");

        var node = builder.AddNodeApp("mynode", tempDir.Path, scriptPath)
            .WithJavaScriptDebuggerProperties(props =>
            {
                props.StopOnEntry = true;
                props.Trace = true;
                props.Timeout = 30000;
            });

        var debuggerPropsAnnotation = node.Resource.Annotations.OfType<ExecutableDebuggerPropertiesAnnotation<JavaScriptDebuggerProperties>>().SingleOrDefault();
        Assert.NotNull(debuggerPropsAnnotation);

        var testProps = new JavaScriptDebuggerProperties
        {
            Name = "Test",
            WorkingDirectory = tempDir.Path
        };

        debuggerPropsAnnotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.StopOnEntry);
        Assert.True(testProps.Trace);
        Assert.Equal(30000, testProps.Timeout);
    }

    [Fact]
    public void ViteAppWithDebugSupport_AddsAnnotationInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), "{}");

        var viteApp = builder.AddViteApp("myvite", tempDir.Path, "dev");

        var annotation = viteApp.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
        Assert.Equal("node", annotation.LaunchConfigurationType);
    }

    [Fact]
    public void JavaScriptAppWithYarn_WithDebugSupport_AddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        File.WriteAllText(Path.Combine(tempDir.Path, "package.json"), "{}");

        var jsApp = builder.AddJavaScriptApp("myjsapp", tempDir.Path, "dev")
            .WithYarn();

        var annotation = jsApp.Resource.Annotations.OfType<SupportsDebuggingAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }
}
