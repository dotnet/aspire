// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public class ResourceSourceViewModelTests
{
    [Theory]
    [MemberData(nameof(ResourceSourceViewModel_ReturnsCorrectValue_TestData))]
    public void ResourceSourceViewModel_ReturnsCorrectValue(TestData testData, ResourceSourceViewModel? expected)
    {
        var properties = new Dictionary<string, ResourcePropertyViewModel>();
        AddStringProperty(KnownProperties.Executable.Path, testData.ExecutablePath);
        AddStringProperty(KnownProperties.Project.Path, testData.ProjectPath);
        AddStringProperty(KnownProperties.Container.Image, testData.ContainerImage);
        AddStringProperty(KnownProperties.Resource.Source, testData.SourceProperty);

        if (testData.ExecutableArguments is not null)
        {
            properties.TryAdd(KnownProperties.Executable.Args, new ResourcePropertyViewModel(KnownProperties.Executable.Args, Value.ForList(testData.ExecutableArguments.Select(Value.ForString).ToArray()), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }

        var resource = ModelTestHelpers.CreateResource(
            resourceType: testData.ResourceType,
            properties: properties);

        var actual = ResourceSourceViewModel.GetSourceViewModel(resource);
        if (expected is null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.ContentAfterValue, actual.ContentAfterValue);
            Assert.Equal(expected.ValueToVisualize, actual.ValueToVisualize);
            Assert.Equal(expected.Tooltip, actual.Tooltip);
        }

        void AddStringProperty(string propertyName, string? propertyValue)
        {
            properties.TryAdd(propertyName, new ResourcePropertyViewModel(propertyName, propertyValue is null ? Value.ForNull() : Value.ForString(propertyValue), false, null, 0, new BrowserTimeProvider(new NullLoggerFactory())));
        }
    }

    public static TheoryData<TestData, ResourceSourceViewModel?> ResourceSourceViewModel_ReturnsCorrectValue_TestData()
    {
        var data = new TheoryData<TestData, ResourceSourceViewModel?>();

        // Project with executable arguments
        data.Add(new TestData(
                ResourceType: "Project",
                ExecutablePath: "path/to/executable",
                ExecutableArguments: ["arg1", "arg2"],
                ProjectPath: "path/to/project",
                ContainerImage: null,
                SourceProperty: null),
            new ResourceSourceViewModel(
                value: "project",
                contentAfterValue: "arg1 arg2",
                valueToVisualize: "path/to/executable arg1 arg2",
                tooltip: "path/to/executable arg1 arg2"));

        // Project without executable arguments
        data.Add(new TestData(
                ResourceType: "Project",
                ExecutablePath: null,
                ExecutableArguments: null,
                ProjectPath: "path/to/project",
                ContainerImage: null,
                SourceProperty: null),
            new ResourceSourceViewModel(
                value: "project",
                contentAfterValue: null,
                valueToVisualize: "path/to/project",
                tooltip: "path/to/project"));

        // Executable with arguments
        data.Add(new TestData(
                ResourceType: "Executable",
                ExecutablePath: "path/to/executable",
                ExecutableArguments: ["arg1", "arg2"],
                ProjectPath: null,
                ContainerImage: null,
                SourceProperty: null),
            new ResourceSourceViewModel(
                value: "executable",
                contentAfterValue: "arg1 arg2",
                valueToVisualize: "path/to/executable arg1 arg2",
                tooltip: "path/to/executable arg1 arg2"));

        // Container image
        data.Add(new TestData(
                ResourceType: "Container",
                ExecutablePath: null,
                ExecutableArguments: null,
                ProjectPath: null,
                ContainerImage: "my-container-image",
                SourceProperty: null),
            new ResourceSourceViewModel(
                value: "my-container-image",
                contentAfterValue: null,
                valueToVisualize: "my-container-image",
                tooltip: "my-container-image"));

        // Resource source property
        data.Add(new TestData(
                ResourceType: "CustomResourceType",
                ExecutablePath: null,
                ExecutableArguments: null,
                ProjectPath: null,
                ContainerImage: null,
                SourceProperty: "source-value"),
            new ResourceSourceViewModel(
                value: "source-value",
                contentAfterValue: null,
                valueToVisualize: "source-value",
                tooltip: "source-value"));

        // Executable path without arguments
        data.Add(new TestData(
                ResourceType: "Executable",
                ExecutablePath: "path/to/executable",
                ExecutableArguments: null,
                ProjectPath: null,
                ContainerImage: null,
                SourceProperty: null),
            new ResourceSourceViewModel(
                value: "executable",
                contentAfterValue: null,
                valueToVisualize: "path/to/executable",
                tooltip: ""));

        return data;
    }

    public record TestData(
        string ResourceType,
        string? ExecutablePath,
        string[]? ExecutableArguments,
        string? ProjectPath,
        string? ContainerImage,
        string? SourceProperty);
}
