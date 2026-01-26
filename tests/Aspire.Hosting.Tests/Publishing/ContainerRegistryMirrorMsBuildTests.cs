// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003

using System.Diagnostics;
using System.Reflection;
using System.Text;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Tests.Publishing;

public sealed class ContainerRegistryMirrorMsBuildTests
{
    [Fact]
    public async Task RegistryMirrorTargetsOverrideContainerBaseImage()
    {
        using var tempDirectory = new TestTempDirectory();

        var projectPath = Path.Combine(tempDirectory.Path, "App.csproj");
        var outputPath = Path.Combine(tempDirectory.Path, "container-base-image.txt");
        var wrapperTargetsPath = Path.Combine(tempDirectory.Path, "wrapper.targets");

        File.WriteAllText(projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <Target Name="ComputeContainerBaseImage">
                <PropertyGroup>
                  <ContainerBaseImage>mcr.microsoft.com/dotnet/runtime:8.0</ContainerBaseImage>
                </PropertyGroup>
              </Target>

              <Target Name="ComputeContainerConfig" DependsOnTargets="ComputeContainerBaseImage" />
            </Project>
            """.Replace("\r\n", "\n"));

        var options = new ContainerRegistryMirrorOptions();
        options.Mirrors["mcr.microsoft.com"] = "docker.artifactory.example.com/mcr-remote";

        var mirrorTargetsPath = InvokeWriteRegistryMirrorTargetsFile(options);

        File.WriteAllText(wrapperTargetsPath,
            $"""
            <Project>
              <Import Project="{EscapeForXmlAttribute(mirrorTargetsPath)}" />
              <Target Name="_AspireMirrorProbeWriteContainerBaseImage" AfterTargets="ComputeContainerConfig">
                <WriteLinesToFile File="{EscapeForXmlAttribute(outputPath)}" Lines="$(ContainerBaseImage)" Overwrite="true" />
              </Target>
            </Project>
            """.Replace("\r\n", "\n"));

        try
        {
            RunDotNetMsBuild(
                workingDirectory: tempDirectory.Path,
                $"msbuild -restore --disable-build-servers -nologo \"{projectPath}\" -t:ComputeContainerConfig -p:CustomAfterMicrosoftCommonTargets=\"{wrapperTargetsPath}\"");

            var containerBaseImage = (await File.ReadAllTextAsync(outputPath)).Trim();
            Assert.Equal("docker.artifactory.example.com/mcr-remote/dotnet/runtime:8.0", containerBaseImage);
        }
        finally
        {
            if (File.Exists(mirrorTargetsPath))
            {
                File.Delete(mirrorTargetsPath);
            }
        }
    }

    private static string InvokeWriteRegistryMirrorTargetsFile(ContainerRegistryMirrorOptions options)
    {
        var method = typeof(ResourceContainerImageManager).GetMethod(
            "WriteRegistryMirrorTargetsFile",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var path = (string?)method!.Invoke(null, [options]);
        Assert.False(string.IsNullOrWhiteSpace(path));

        return path!;
    }

    private static void RunDotNetMsBuild(string workingDirectory, string arguments)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                error.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        Assert.True(process.WaitForExit(milliseconds: 180_000), $"dotnet msbuild timed out. Output:{Environment.NewLine}{output}{Environment.NewLine}Error:{Environment.NewLine}{error}");
        Assert.True(process.ExitCode == 0, $"dotnet msbuild failed with exit code {process.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{output}{Environment.NewLine}Error:{Environment.NewLine}{error}");
    }

    private static string EscapeForXmlAttribute(string value)
        => value.Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
}
