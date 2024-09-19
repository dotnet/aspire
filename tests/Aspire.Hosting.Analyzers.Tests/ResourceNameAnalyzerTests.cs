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

        await test.RunAsync();
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

        await test.RunAsync();
    }
}
