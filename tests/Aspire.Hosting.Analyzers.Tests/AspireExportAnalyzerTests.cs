// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Aspire.Hosting.Analyzers.Tests;

public class AspireExportAnalyzerTests
{
    [Fact]
    public async Task ValidExport_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/testMethod@1", Description = "Test method")]
                public static string TestMethod() => "test";
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidExportWithPackagePrefix_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire.redis/addRedis@1", Description = "Add Redis")]
                public static string AddRedis() => "test";
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidExportWithDottedOperation_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/Dictionary.set@1", Description = "Dictionary set")]
                public static void DictionarySet() { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task InstanceMethod_ReportsASPIRE007()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_exportMethodMustBeStatic;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public class TestExports
            {
                [AspireExport("aspire/instanceMethod@1")]
                public string InstanceMethod() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Method 'InstanceMethod' marked with [AspireExport] must be static")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidIdFormat_MissingVersion_ReportsASPIRE008()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_invalidExportIdFormat;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/noVersion")]
                public static string NoVersion() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Export ID 'aspire/noVersion' does not match the required format 'aspire/{operation}@{version}' or 'aspire.{package}/{operation}@{version}'")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidIdFormat_InvalidPrefix_ReportsASPIRE008()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_invalidExportIdFormat;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("invalid/prefix@1")]
                public static string InvalidPrefix() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Export ID 'invalid/prefix@1' does not match the required format 'aspire/{operation}@{version}' or 'aspire.{package}/{operation}@{version}'")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidReturnType_ReportsASPIRE009()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_returnTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/invalidReturn@1")]
                public static Stream InvalidReturn() => Stream.Null;
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(8, 6).WithMessage("Method 'InvalidReturn' has return type 'System.IO.Stream' which is not ATS-compatible. Use void, Task, Task<T>, or a supported Aspire type.")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidParameterType_ReportsASPIRE010()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_parameterTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/invalidParam@1")]
                public static void InvalidParam(Stream stream) { }
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(8, 6).WithMessage("Parameter 'stream' of type 'System.IO.Stream' in method 'InvalidParam' is not ATS-compatible. Use primitive types, enums, or supported Aspire types.")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidPrimitiveTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/primitives@1")]
                public static int Primitives(string s, int i, bool b, double d, long l) => i;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidNullableTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/nullables@1")]
                public static int? Nullables(int? i, bool? b) => i;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidTaskReturn_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.Threading.Tasks;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/asyncMethod@1")]
                public static Task AsyncMethod() => Task.CompletedTask;

                [AspireExport("aspire/asyncMethodWithResult@1")]
                public static Task<string> AsyncMethodWithResult() => Task.FromResult("test");
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidEnumTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public enum MyEnum { A, B, C }

            public static class TestExports
            {
                [AspireExport("aspire/enumMethod@1")]
                public static MyEnum EnumMethod(MyEnum value) => value;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidDelegateParameter_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/withCallback@1")]
                public static void WithCallback(Func<string, int> callback) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidBuilderParameter_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/builderMethod@1")]
                public static void BuilderMethod(IDistributedApplicationBuilder builder) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidParamsArray_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/paramsMethod@1")]
                public static void ParamsMethod(params string[] args) { }
            }
            """, []);

        await test.RunAsync();
    }
}
