// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Aspire.Hosting.Analyzers.Tests;

public class ResourceNameAnalyzerTests
{
    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task ResourceNameInvalid(string resourceName)
    {
        Assert.False(ModelName.TryValidateName("Resource", resourceName, out var message));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddParameter("{{resourceName}}");
            """,
            [CompilerError(diagnostic.Id).WithLocation(5, 22).WithMessage(message)]);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.InvalidModelNames))]
    public async Task ResourceNameInvalidMultipleParameters(string resourceName)
    {
        Assert.False(ModelName.TryValidateName("Resource", $"{resourceName}-one", out var message1));
        Assert.False(ModelName.TryValidateName("Resource", $"{resourceName}-two", out var message2));

        var diagnostic = AppHostAnalyzer.Diagnostics.s_modelNameMustBeValid;

        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddMultipleParameters(
                "{{resourceName}}-one",
                "{{resourceName}}-two");

            public static class TestExtensions
            {
                public static void AddMultipleParameters(this IDistributedApplicationBuilder builder, [ResourceName] string param1Name, [ResourceName] string param2Name)
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
    [ClassData(typeof(TestData.ValidModelNames))]
    public async Task ResourceNameValid(string resourceName)
    {
        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddParameter("{{resourceName}}");
            """, []);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [ClassData(typeof(TestData.ValidModelNames))]
    public async Task ResourceNameValidMultipleParameters(string resourceName)
    {
        var test = AnalyzerTest.Create<AppHostAnalyzer>($$"""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            builder.AddMultipleParameters(
                "{{resourceName}}-one",
                "{{resourceName}}-two");

            public static class TestExtensions
            {
                public static void AddMultipleParameters(this IDistributedApplicationBuilder builder, [ResourceName] string param1Name, [ResourceName] string param2Name)
                {

                }
            }
            """, []);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
