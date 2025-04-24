// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Aspire.Hosting.Analyzers.Tests;

public class CombinationsAnalyzerTests
{
    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task MethodWithBothResourceAndEndpointNameParametersInvalid(string modelName)
    {
        Assert.False(ModelName.TryValidateName("Resource", $"{modelName}-resource", out var message1));
        Assert.False(ModelName.TryValidateName("Endpoint", $"{modelName}-endpoint", out var message2));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddMultiple(
                "{{modelName}}-resource",
                "{{modelName}}-endpoint");

            public static class TestExtensions
            {
                public static void AddMultiple(this IDistributedApplicationBuilder builder, [ResourceName] string param1Name, [EndpointName] string param2Name)
                {

                }
            }
            """,
            [
                CompilerError(diagnostic.Id).WithLocation(7, 5).WithMessage(message1),
                CompilerError(diagnostic.Id).WithLocation(8, 5).WithMessage(message2)
            ]);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task AnalyzerIsResilientToParameterWithMutlipleModelNameAttributes(string modelName)
    {
        Assert.False(ModelName.TryValidateName("Resource", $"{modelName}", out var message1));
        Assert.False(ModelName.TryValidateName("Endpoint", $"{modelName}", out var message2));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddMultiple("{{modelName}}");

            public static class TestExtensions
            {
                public static void AddMultiple(this IDistributedApplicationBuilder builder, [ResourceName, EndpointName] string theName)
                {

                }
            }
            """,
            [
                CompilerError(diagnostic.Id).WithLocation(6, 21).WithMessage(message1),
                CompilerError(diagnostic.Id).WithLocation(6, 21).WithMessage(message2)
            ]);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
