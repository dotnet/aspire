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
                [AspireExport("testMethod", Description = "Test method")]
                public static string TestMethod() => "test";
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidExportWithCamelCase_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("addRedis", Description = "Add Redis")]
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
                [AspireExport("Dictionary.set", Description = "Dictionary set")]
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
                [AspireExport("instanceMethod")]
                public string InstanceMethod() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Method 'InstanceMethod' marked with [AspireExport] must be static")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidIdFormat_WithSlash_ReportsASPIRE008()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_invalidExportIdFormat;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("aspire/methodName")]
                public static string MethodWithSlash() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Export ID 'aspire/methodName' is not a valid method name. Use a valid identifier (e.g., 'addRedis', 'withEnvironment').")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidIdFormat_WithAtSymbol_ReportsASPIRE008()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_invalidExportIdFormat;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("method@1")]
                public static string MethodWithAt() => "test";
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(7, 6).WithMessage("Export ID 'method@1' is not a valid method name. Use a valid identifier (e.g., 'addRedis', 'withEnvironment').")]);

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
                [AspireExport("invalidReturn")]
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
                [AspireExport("invalidParam")]
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
                [AspireExport("primitives")]
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
                [AspireExport("nullables")]
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
                [AspireExport("asyncMethod")]
                public static Task AsyncMethod() => Task.CompletedTask;

                [AspireExport("asyncMethodWithResult")]
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
                [AspireExport("enumMethod")]
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
                [AspireExport("withCallback")]
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
                [AspireExport("builderMethod")]
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
                [AspireExport("paramsMethod")]
                public static void ParamsMethod(params string[] args) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidCollectionTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.Collections.Generic;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("listMethod")]
                public static List<string> ListMethod(Dictionary<string, int> dict) => new();

                [AspireExport("readonlyCollections")]
                public static IReadOnlyList<int> ReadonlyMethod(IReadOnlyDictionary<string, bool> dict) => [];
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidDateOnlyTimeOnly_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("dateTimeMethod")]
                public static DateOnly DateMethod(TimeOnly time, DateTimeOffset dto, TimeSpan ts) => DateOnly.MinValue;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task TypeWithAspireExportAttribute_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            [AspireExport]
            public class MyCustomType
            {
                public string Name { get; set; } = "";
            }

            public static class TestExports
            {
                [AspireExport("customTypeMethod")]
                public static void Method(MyCustomType custom) { }

                [AspireExport("returnsCustomType")]
                public static MyCustomType ReturnsCustom() => new();
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidObjectType_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("objectMethod")]
                public static object ObjectMethod(object value) => value;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidArrayTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("arrayMethod")]
                public static string[] ArrayMethod(int[] numbers) => [];
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidCollectionElementType_ReportsASPIRE010()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_parameterTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.Collections.Generic;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("invalidList")]
                public static void InvalidList(List<Stream> streams) { }
            }
            """,
            [CompilerError(diagnostic.Id).WithLocation(9, 6).WithMessage("Parameter 'streams' of type 'System.Collections.Generic.List<System.IO.Stream>' in method 'InvalidList' is not ATS-compatible. Use primitive types, enums, or supported Aspire types.")]);

        await test.RunAsync();
    }
}
