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

        await test.RunAsync();
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

        await test.RunAsync();
    }
}
