// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Aspire.Hosting.Analyzers.Tests;

public class ResourceSourceLocationGeneratorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CapturesTheExecutedBranch(bool condition)
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var builder = new ApplicationBuilder();
                    if (condition)
                    {
                        return builder.AddResource("same", 1).Resource;
                    }
                    else
                    {
                        return builder.AddResource("same", 2).Resource;
                    }
                }
            }
            """ + Types;

        AssertSourceLocation(source, condition, condition ? "AddResource(\"same\", 1)" : "AddResource(\"same\", 2)");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DistinguishesCallsOnTheSameLine(bool condition)
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var builder = new ApplicationBuilder();
                    return condition ? builder.AddResource("same", 1).Resource : builder.AddResource("same", 2).Resource;
                }
            }
            """ + Types;

        AssertSourceLocation(source, condition, condition ? "AddResource(\"same\", 1)" : "AddResource(\"same\", 2)");
    }

    [Fact]
    public void FluentCallsDoNotReplaceTheCreationLocation()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var builder = new ApplicationBuilder();
                    return builder.AddResource("same", 1).AddTag("some-tag").Resource;
                }
            }
            """ + Types;

        AssertSourceLocation(source, true, "AddResource(\"same\", 1)");
    }

    [Fact]
    public void PreservesGenericMethodConstraintsAndNamedArguments()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var builder = new ApplicationBuilder();
                    return builder.AddGeneric<Resource>(name: "same").Resource;
                }
            }
            """ + Types;

        AssertSourceLocation(source, true, "AddGeneric<Resource>");
    }

    [Fact]
    public void ConfigurationCallsDoNotClaimAnUntrackedResource()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    IResourceBuilder<Resource> existing = new ResourceBuilder<Resource>(new Resource());
                    return existing.AddTag("same").Resource;
                }
            }
            """ + Types;

        var resource = Execute(source, true, expectInterception: true)!;
        var annotations = (System.Collections.IEnumerable)resource.GetType().GetProperty("Annotations")!.GetValue(resource)!;

        Assert.Empty(annotations);
    }

    [Fact]
    public void PreservesBaseQualifiedVirtualDispatch()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static object Run(bool condition) =>
                    new ChildFactory().RegisterParent().Resource.Annotations[0];
            }
            public class ParentFactory
            {
                public virtual IResourceBuilder<Resource> AddWorker(string name) =>
                    new ResourceBuilder<Resource>(new Resource { Annotations = new() { "parent" } });
            }
            public sealed class ChildFactory : ParentFactory
            {
                public override IResourceBuilder<Resource> AddWorker(string name) =>
                    new ResourceBuilder<Resource>(new Resource { Annotations = new() { "child" } });
                public IResourceBuilder<Resource> RegisterParent() => base.AddWorker("same");
            }
            """ + Types;

        Assert.Equal("parent", Execute(source, true, expectInterception: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoesNotInterceptLocalFactories(bool isStatic)
    {
        var source = $$"""
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var resource = new Resource();
                    {{(isStatic ? "static " : "")}}IResourceBuilder<Resource> AddWorker(Resource value) =>
                        new ResourceBuilder<Resource>({{(isStatic ? "value" : "resource")}});
                    return AddWorker(resource).Resource;
                }
            }
            """ + Types;

        var resource = Execute(source, true, expectInterception: false)!;
        var annotations = (System.Collections.IEnumerable)resource.GetType().GetProperty("Annotations")!.GetValue(resource)!;

        Assert.Empty(annotations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoesNotInterceptStaticAbstractOrVirtualInterfaceFactories(bool isAbstract)
    {
        var source = $$"""
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition) => Create<Factory>().Resource;
                private static IResourceBuilder<Resource> Create<T>() where T : IFactory<T> => T.AddWorker("same");
            }
            public interface IFactory<T> where T : IFactory<T>
            {
                static {{(isAbstract ? "abstract" : "virtual")}} IResourceBuilder<Resource> AddWorker(string name)
                    {{(isAbstract ? ";" : "=> new ResourceBuilder<Resource>(new Resource());")}}
            }
            public sealed class Factory : IFactory<Factory>
            {
                public static IResourceBuilder<Resource> AddWorker(string name) =>
                    new ResourceBuilder<Resource>(new Resource());
            }
            """ + Types;

        var resource = Execute(source, true, expectInterception: false)!;
        var annotations = (System.Collections.IEnumerable)resource.GetType().GetProperty("Annotations")!.GetValue(resource)!;

        Assert.Empty(annotations);
    }

    [Fact]
    public void CapturesStaticInterfaceFactories()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition) => IFactory.AddWorker("same").Resource;
            }
            public interface IFactory
            {
                public static IResourceBuilder<Resource> AddWorker(string name) =>
                    new ResourceBuilder<Resource>(new Resource());
            }
            """ + Types;

        AssertSourceLocation(source, true, "AddWorker(\"same\")");
    }

    [Fact]
    public void DoesNotRequireInterceptionInOlderCompilerConfigurations()
    {
        var compilation = CreateCompilation("public class Program { }", CSharpParseOptions.Default, Path.GetFullPath("SourceLocation.AppHost.cs"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ResourceSourceLocationGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        Assert.Empty(driver.GetRunResult().GeneratedTrees);
        Assert.Empty(driver.GetRunResult().Diagnostics);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SourceLocation.AppHost.cs")]
    [InlineData("C:SourceLocation.AppHost.cs")]
    [InlineData("\\SourceLocation.AppHost.cs")]
    public void DoesNotCaptureSourceWithoutAnAbsoluteFilePath(string filePath)
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition) =>
                    new ApplicationBuilder().AddResource("same", 1).Resource;
            }
            """ + Types;
        var options = CSharpParseOptions.Default
            .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Aspire.Hosting.Generated")]);
        var compilation = CreateCompilation(source, options, filePath);
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new ResourceSourceLocationGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().GeneratedTrees);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
    }

    [Fact]
    public void PreservesANullableFactoryResult()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource? Run(bool condition) =>
                    new ApplicationBuilder().AddNullable("same")?.Resource;
            }
            """ + Types;

        Assert.Null(Execute(source, true, expectInterception: true));
    }

    [Fact]
    public void SupportsResourceBuilderImplementationsWithoutCovariance()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition)
                {
                    var builder = new ApplicationBuilder();
                    var result = builder.AddValueResource("same");
                    return new Resource { Annotations = result.Resource.Annotations };
                }
            }
            """ + Types;

        AssertSourceLocation(source, true, "AddValueResource(\"same\")");
    }

    [Theory]
    [InlineData("System.Obsolete(\"Use another factory.\")", "CS0618", false)]
    [InlineData("System.Obsolete(\"Use another factory.\", DiagnosticId = \"SOURCE001\")", "SOURCE001", false)]
    [InlineData("System.Diagnostics.CodeAnalysis.Experimental(\"SOURCE002\")", "SOURCE002", false)]
    [InlineData("System.Diagnostics.CodeAnalysis.Experimental(\"SOURCE002\")", "SOURCE002", true)]
    public void PreservesOptInToDeprecatedOrExperimentalFactories(string attribute, string diagnosticId, bool annotateResource)
    {
        var source = $$"""
            #pragma warning disable {{diagnosticId}}
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition) =>
                    new ApplicationBuilder().AddMarked("same").Resource;
            }
            public static class MarkedExtensions
            {
                {{(annotateResource ? "" : $"[{attribute}]")}}
                public static IResourceBuilder<Resource> AddMarked(this ApplicationBuilder builder, string name) =>
                    new ResourceBuilder<Resource>(new Resource());
            }
            """ + (annotateResource
                ? Types.Replace("public class Resource : IResource", $"[{attribute}]\npublic class Resource : IResource", StringComparison.Ordinal)
                : Types);

        AssertSourceLocation(source, true, "AddMarked(\"same\")");
    }

    [Fact]
    public void GeneratedWarningSuppressionDoesNotBypassExperimentalOptIn()
    {
        var source = """
            using Aspire.Hosting.ApplicationModel;
            public static class SourceCases
            {
                public static Resource Run(bool condition) =>
                    new ApplicationBuilder().AddMarked("same").Resource;
            }
            public static class MarkedExtensions
            {
                [System.Diagnostics.CodeAnalysis.Experimental("SOURCE002")]
                public static IResourceBuilder<Resource> AddMarked(this ApplicationBuilder builder, string name) =>
                    new ResourceBuilder<Resource>(new Resource());
            }
            """ + Types;
        var options = CSharpParseOptions.Default
            .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Aspire.Hosting.Generated")]);
        var compilation = CreateCompilation(source, options, Path.GetFullPath("SourceLocation.AppHost.cs"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new ResourceSourceLocationGenerator().AsSourceGenerator()], parseOptions: options);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Collection(output.GetDiagnostics(), diagnostic =>
        {
            Assert.Equal("SOURCE002", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Same(compilation.SyntaxTrees.Single(), diagnostic.Location.SourceTree);
        });
    }

    private static object? Execute(string source, bool condition, bool expectInterception)
    {
        var options = CSharpParseOptions.Default
            .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Aspire.Hosting.Generated")]);
        var compilation = CreateCompilation(source, options, Path.GetFullPath("SourceLocation.AppHost.cs"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new ResourceSourceLocationGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(expectInterception ? 1 : 0, driver.GetRunResult().GeneratedTrees.Length);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());

        return assembly.GetType("SourceCases")!.GetMethod("Run")!.Invoke(null, [condition]);
    }

    private static void AssertSourceLocation(string source, bool condition, string invocation)
    {
        var resource = Execute(source, condition, expectInterception: true)!;
        var annotations = (System.Collections.IEnumerable)resource.GetType().GetProperty("Annotations")!.GetValue(resource)!;
        var annotation = Assert.Single(annotations.Cast<object>());
        var type = annotation.GetType();
        var position = SourceText.From(source).Lines.GetLinePosition(source.IndexOf(invocation, StringComparison.Ordinal));

        Assert.Equal(Path.GetFullPath("SourceLocation.AppHost.cs"), type.GetProperty("FilePath")!.GetValue(annotation));
        Assert.Equal(position.Line + 1, type.GetProperty("LineNumber")!.GetValue(annotation));
        Assert.Equal(position.Character + 1, type.GetProperty("ColumnNumber")!.GetValue(annotation));
        Assert.StartsWith("fnv1a64:", (string)type.GetProperty("DocumentHash")!.GetValue(annotation)!);
    }

    private static CSharpCompilation CreateCompilation(string source, CSharpParseOptions options, string filePath) =>
        CSharpCompilation.Create($"SourceLocationTests_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source, options, filePath)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable,
                generalDiagnosticOption: ReportDiagnostic.Error));

    private const string Types = """

        namespace Aspire.Hosting.ApplicationModel
        {
            public interface IResource { System.Collections.Generic.List<object> Annotations { get; } }
            public interface IResourceBuilder<out T> where T : IResource { T Resource { get; } }
            public class Resource : IResource
            {
                public System.Collections.Generic.List<object> Annotations { get; init; } = new();
            }
            public struct ValueResource : IResource
            {
                public System.Collections.Generic.List<object> Annotations { get; init; }
            }
            public sealed class ResourceBuilder<T>(T resource) : IResourceBuilder<T> where T : IResource
            {
                public T Resource { get; } = resource;
            }
            public sealed class ApplicationBuilder { }
            public static class Extensions
            {
                public static IResourceBuilder<Resource> AddResource(this ApplicationBuilder builder, string name, int value) =>
                    new ResourceBuilder<Resource>(new Resource());
                public static IResourceBuilder<T> AddGeneric<T>(this ApplicationBuilder builder, string name) where T : Resource, new() =>
                    new ResourceBuilder<T>(new T());
                public static IResourceBuilder<T> AddTag<T>(this IResourceBuilder<T> builder, string tag) where T : IResource => builder;
                public static IResourceBuilder<Resource>? AddNullable(this ApplicationBuilder builder, string name) => null;
                public static IResourceBuilder<ValueResource> AddValueResource(this ApplicationBuilder builder, string name) =>
                    new ResourceBuilder<ValueResource>(new ValueResource { Annotations = new() });
            }
            public sealed class ResourceSourceLocationAnnotation(string filePath, int lineNumber, int? columnNumber, string documentHash)
            {
                public string FilePath { get; } = filePath;
                public int LineNumber { get; } = lineNumber;
                public int? ColumnNumber { get; } = columnNumber;
                public string DocumentHash { get; } = documentHash;
                public string? MethodName { get; init; }
            }
        }
        """;
}
