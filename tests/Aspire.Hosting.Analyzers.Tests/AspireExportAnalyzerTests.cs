// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
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

    // ASPIRE011 Tests - Union requires at least 2 types

    [Fact]
    public async Task UnionWithSingleType_ReportsASPIRE011()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_unionRequiresAtLeastTwoTypes;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("singleUnion")]
                public static void SingleUnion([AspireUnion(typeof(string))] object value) { }
            }
            """,
            [new DiagnosticResult(diagnostic).WithLocation(8, 37).WithArguments("1")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithEmptyTypes_ReportsASPIRE011()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_unionRequiresAtLeastTwoTypes;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("emptyUnion")]
                public static void EmptyUnion([AspireUnion()] object value) { }
            }
            """,
            [new DiagnosticResult(diagnostic).WithLocation(8, 36).WithArguments("0")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidUnionWithTwoTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("validUnion")]
                public static void ValidUnion([AspireUnion(typeof(string), typeof(int))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task ValidUnionWithMultipleTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("multiUnion")]
                public static void MultiUnion([AspireUnion(typeof(string), typeof(int), typeof(bool))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    // ASPIRE012 Tests - Union types must be ATS-compatible

    [Fact]
    public async Task UnionWithIncompatibleType_ReportsASPIRE012()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_unionTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("invalidUnion")]
                public static void InvalidUnion([AspireUnion(typeof(string), typeof(Stream))] object value) { }
            }
            """,
            [new DiagnosticResult(diagnostic).WithLocation(9, 38).WithArguments("System.IO.Stream")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithPlainClass_ReportsASPIRE012()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_unionTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public class MyPlainClass { }

            public static class TestExports
            {
                [AspireExport("plainClassUnion")]
                public static void PlainClassUnion([AspireUnion(typeof(string), typeof(MyPlainClass))] object value) { }
            }
            """,
            [new DiagnosticResult(diagnostic).WithLocation(10, 41).WithArguments("MyPlainClass")]);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithDtoType_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            [AspireDto]
            public class MyDtoType { public string Name { get; set; } = ""; }

            public static class TestExports
            {
                [AspireExport("dtoUnion")]
                public static void DtoUnion([AspireUnion(typeof(string), typeof(MyDtoType))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithPrimitives_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("primitiveUnion")]
                public static void PrimitiveUnion([AspireUnion(typeof(string), typeof(int), typeof(bool))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    // ASPIRE013 Tests - No duplicate export IDs for same target type

    [Fact]
    public async Task DuplicateExportIdSameTargetType_ReportsASPIRE013()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_duplicateExportId;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("addThing")]
                public static void AddThing(this IDistributedApplicationBuilder builder, string name) { }

                [AspireExport("addThing")]
                public static void AddThingWithPort(this IDistributedApplicationBuilder builder, string name, int port) { }
            }
            """,
            [
                new DiagnosticResult(diagnostic).WithLocation(7, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder"),
                new DiagnosticResult(diagnostic).WithLocation(10, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder")
            ]);

        await test.RunAsync();
    }

    [Fact]
    public async Task DifferentExportIdsSameTargetType_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("addThing")]
                public static void AddThing(this IDistributedApplicationBuilder builder, string name) { }

                [AspireExport("addThingWithPort")]
                public static void AddThingWithPort(this IDistributedApplicationBuilder builder, string name, int port) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task SameExportIdDifferentTargetTypes_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;
            using System.Collections.Generic;

            var builder = DistributedApplication.CreateBuilder(args);

            public class MyResource : IResource
            {
                public string Name => "test";
                public ResourceAnnotationCollection Annotations { get; } = new();
            }

            public static class TestExports
            {
                [AspireExport("configure")]
                public static void ConfigureBuilder(this IDistributedApplicationBuilder builder, string name) { }

                [AspireExport("configure")]
                public static IResourceBuilder<MyResource> ConfigureResource(this IResourceBuilder<MyResource> builder, string value)
                    => builder;
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task NonExtensionMethod_NoDuplicateCheck()
    {
        // Non-extension methods with same export ID should not trigger ASPIRE013
        // since they don't have a target type in the same way
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("getValue")]
                public static string GetValue() => "test";

                [AspireExport("getValue")]
                public static string GetValueWithDefault(string defaultValue) => defaultValue;
            }
            """, []);

        await test.RunAsync();
    }

    // Additional ASPIRE012 tests - valid ATS types in unions

    [Fact]
    public async Task UnionWithIResource_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using Aspire.Hosting.ApplicationModel;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("resourceUnion")]
                public static void ResourceUnion([AspireUnion(typeof(string), typeof(IResource))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithEnum_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public enum MyEnum { A, B, C }

            public static class TestExports
            {
                [AspireExport("enumUnion")]
                public static void EnumUnion([AspireUnion(typeof(string), typeof(MyEnum))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithAspireExportType_NoDiagnostics()
    {
        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            [AspireExport]
            public class MyExportType { public string Name { get; set; } = ""; }

            public static class TestExports
            {
                [AspireExport("exportTypeUnion")]
                public static void ExportTypeUnion([AspireUnion(typeof(string), typeof(MyExportType))] object value) { }
            }
            """, []);

        await test.RunAsync();
    }

    [Fact]
    public async Task UnionWithMultipleInvalidTypes_ReportsMultipleASPIRE012()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_unionTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public class MyPlainClass { }

            public static class TestExports
            {
                [AspireExport("multiInvalid")]
                public static void MultiInvalid([AspireUnion(typeof(Stream), typeof(MyPlainClass))] object value) { }
            }
            """,
            [
                new DiagnosticResult(diagnostic).WithLocation(11, 38).WithArguments("System.IO.Stream"),
                new DiagnosticResult(diagnostic).WithLocation(11, 38).WithArguments("MyPlainClass")
            ]);

        await test.RunAsync();
    }

    // Additional ASPIRE013 tests - cross-class and multiple duplicates

    [Fact]
    public async Task DuplicateExportIdAcrossClasses_ReportsASPIRE013()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_duplicateExportId;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class ClassA
            {
                [AspireExport("addThing")]
                public static void AddThing(this IDistributedApplicationBuilder builder, string name) { }
            }

            public static class ClassB
            {
                [AspireExport("addThing")]
                public static void AddThingAlso(this IDistributedApplicationBuilder builder, string name) { }
            }
            """,
            [
                new DiagnosticResult(diagnostic).WithLocation(7, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder"),
                new DiagnosticResult(diagnostic).WithLocation(13, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder")
            ]);

        await test.RunAsync();
    }

    [Fact]
    public async Task ThreeOrMoreDuplicates_ReportsAllASPIRE013()
    {
        var diagnostic = AspireExportAnalyzer.Diagnostics.s_duplicateExportId;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("addThing")]
                public static void AddThing1(this IDistributedApplicationBuilder builder) { }

                [AspireExport("addThing")]
                public static void AddThing2(this IDistributedApplicationBuilder builder) { }

                [AspireExport("addThing")]
                public static void AddThing3(this IDistributedApplicationBuilder builder) { }
            }
            """,
            [
                new DiagnosticResult(diagnostic).WithLocation(7, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder"),
                new DiagnosticResult(diagnostic).WithLocation(10, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder"),
                new DiagnosticResult(diagnostic).WithLocation(13, 6).WithArguments("addThing", "Aspire.Hosting.IDistributedApplicationBuilder")
            ]);

        await test.RunAsync();
    }

    // Combined ASPIRE011 + ASPIRE012 test

    [Fact]
    public async Task SingleInvalidType_ReportsBothASPIRE011AndASPIRE012()
    {
        var asp011 = AspireExportAnalyzer.Diagnostics.s_unionRequiresAtLeastTwoTypes;
        var asp012 = AspireExportAnalyzer.Diagnostics.s_unionTypeMustBeAtsCompatible;

        var test = AnalyzerTest.Create<AspireExportAnalyzer>("""
            using Aspire.Hosting;
            using System.IO;

            var builder = DistributedApplication.CreateBuilder(args);

            public static class TestExports
            {
                [AspireExport("singleInvalid")]
                public static void SingleInvalid([AspireUnion(typeof(Stream))] object value) { }
            }
            """,
            [
                new DiagnosticResult(asp011).WithLocation(9, 39).WithArguments("1"),
                new DiagnosticResult(asp012).WithLocation(9, 39).WithArguments("System.IO.Stream")
            ]);

        await test.RunAsync();
    }
}
