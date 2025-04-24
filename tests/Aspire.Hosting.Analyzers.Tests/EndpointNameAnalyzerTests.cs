// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Aspire.Hosting.Analyzers.Tests;

public class EndpointNameAnalyzerTests
{
    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task EndpointNameInvalid(string endpointName)
    {
        Assert.False(ModelName.TryValidateName("Endpoint", endpointName, out var message));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddContainer("container", "image")
                .WithEndpoint(port: 1234, name: "{{endpointName}}");
            """,
            [CompilerError(diagnostic.Id).WithLocation(6, 37).WithMessage(message)]);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task EndpointNameInvalidMultipleParameters(string endpointName)
    {
        Assert.False(ModelName.TryValidateName("Endpoint", $"{endpointName}-one", out var message1));
        Assert.False(ModelName.TryValidateName("Endpoint", $"{endpointName}-two", out var message2));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddContainer("container", "image")
                .WithEndpoints(
                    "{{endpointName}}-one",
                    "{{endpointName}}-two");

            public static class TestExtensions
            {
                public static IResourceBuilder<ContainerResource> WithEndpoints(
                    this IResourceBuilder<ContainerResource> builder,
                    [EndpointName] string param1Name,
                    [EndpointName] string param2Name)
                {
                    return builder;
                }
            }
            """,
            [
                CompilerError(diagnostic.Id).WithLocation(8, 9).WithMessage(message1),
                CompilerError(diagnostic.Id).WithLocation(9, 9).WithMessage(message2)
            ]);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.ValidModelNames))]
    public async Task EndpointNameValid(string endpointName)
    {
        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddContainer("container", "image")
                .WithEndpoint(port: 1234, name: "{{endpointName}}");
            """, []);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.ValidModelNames))]
    public async Task EndpointNameValidMultipleParameters(string endpointName)
    {
        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddContainer("container", "image")
                .WithEndpoints(
                    "{{endpointName}}-one",
                    "{{endpointName}}-two");

            public static class TestExtensions
            {
                public static IResourceBuilder<ContainerResource> WithEndpoints(
                    this IResourceBuilder<ContainerResource> builder,
                    [EndpointName] string param1Name,
                    [EndpointName] string param2Name)
                {
                    return builder;
                }
            }
            """, []);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
