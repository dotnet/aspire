// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using Aspire.Managed;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class RemoteHostLoadContextTests
{
    [Fact]
    public void SharedAssemblies_NotInLibsDir_FallBackToDefault()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(emptyDir);

        try
        {
            var alc = new RemoteHostLoadContext(
                typeof(RemoteHostLoadContextTests).Assembly,
                ["Microsoft.Extensions.Logging.Abstractions"],
                emptyDir);

            var asm = alc.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));

            var defaultAsm = AssemblyLoadContext.Default.LoadFromAssemblyName(
                new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));
            Assert.Same(defaultAsm, asm);
        }
        finally
        {
            Directory.Delete(emptyDir, recursive: true);
        }
    }

    [Fact]
    public void AspireHosting_LoadsFromLibsDir_WhenPresent()
    {
        var libsPath = AppContext.BaseDirectory;
        var alc = new RemoteHostLoadContext(typeof(RemoteHostLoadContextTests).Assembly, [], libsPath);

        var asm = alc.LoadFromAssemblyName(new AssemblyName("Aspire.Hosting"));

        var defaultAsm = typeof(Aspire.Hosting.Ats.ICodeGenerator).Assembly;
        Assert.NotSame(defaultAsm, asm);
        Assert.Same(alc, AssemblyLoadContext.GetLoadContext(asm));
    }

    [Fact]
    public void NonSharedAssemblies_LoadFromLibsDir_InRemoteHostContext()
    {
        var libsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(libsPath);
        RemoteHostLoadContext? alc = null;
        WeakReference? alcRef = null;

        try
        {
            var sourceAssemblyPath = typeof(RemoteHostLoadContextTests).Assembly.Location;
            var destinationAssemblyPath = Path.Combine(libsPath, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath);

            alc = new RemoteHostLoadContext(typeof(RemoteHostLoadContextTests).Assembly, [], libsPath);
            alcRef = new WeakReference(alc);

            var isolatedAsm = alc.LoadFromAssemblyName(new AssemblyName(typeof(RemoteHostLoadContextTests).Assembly.GetName().Name!));

            Assert.NotSame(typeof(RemoteHostLoadContextTests).Assembly, isolatedAsm);
            Assert.Same(alc, AssemblyLoadContext.GetLoadContext(isolatedAsm));
        }
        finally
        {
            alc?.Unload();

            UnloadAndCollect(alcRef);

            if (alcRef is null || !alcRef.IsAlive)
            {
                Directory.Delete(libsPath, recursive: true);
            }
        }
    }

    [Fact]
    public void ExplicitSharedAssemblies_AreShared()
    {
        var alc = new RemoteHostLoadContext(
            typeof(RemoteHostLoadContextTests).Assembly,
            [
                "System.Diagnostics.DiagnosticSource",
                "System.Diagnostics.EventLog",
                "System.Text.Json",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Hosting",
                "Microsoft.AspNetCore.Authorization"
            ]);

        Assert.True(alc.ShouldShareAssembly("System.Diagnostics.DiagnosticSource"));
        Assert.True(alc.ShouldShareAssembly("System.Diagnostics.EventLog"));
        Assert.True(alc.ShouldShareAssembly("System.Text.Json"));
        Assert.True(alc.ShouldShareAssembly("Microsoft.Extensions.DependencyInjection.Abstractions"));
        Assert.True(alc.ShouldShareAssembly("Microsoft.Extensions.Logging.Abstractions"));
        Assert.True(alc.ShouldShareAssembly("Microsoft.Extensions.Hosting"));
        Assert.True(alc.ShouldShareAssembly("Microsoft.AspNetCore.Authorization"));
    }

    [Fact]
    public void SharedFrameworkAssemblies_LoadFromDefault_WhenPresentInLibsDir()
    {
        var libsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(libsPath);

        try
        {
            var sourceAssemblyPath = typeof(System.Diagnostics.DiagnosticListener).Assembly.Location;
            var destinationAssemblyPath = Path.Combine(libsPath, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath);

            var alc = new RemoteHostLoadContext(typeof(RemoteHostLoadContextTests).Assembly, ["System.Diagnostics.DiagnosticSource"], libsPath);

            var sharedAsm = alc.LoadFromAssemblyName(new AssemblyName("System.Diagnostics.DiagnosticSource"));

            Assert.Same(typeof(System.Diagnostics.DiagnosticListener).Assembly, sharedAsm);
            Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(sharedAsm));
        }
        finally
        {
            Directory.Delete(libsPath, recursive: true);
        }
    }

    [Fact]
    public void SharedMicrosoftExtensionsAssemblies_LoadFromDefault_WhenPresentInLibsDir()
    {
        var libsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(libsPath);

        try
        {
            var sourceAssemblyPath = typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location;
            var destinationAssemblyPath = Path.Combine(libsPath, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath);

            var alc = new RemoteHostLoadContext(typeof(RemoteHostLoadContextTests).Assembly, ["Microsoft.Extensions.Logging.Abstractions"], libsPath);

            var sharedAsm = alc.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));

            Assert.Same(typeof(Microsoft.Extensions.Logging.ILogger).Assembly, sharedAsm);
            Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(sharedAsm));
        }
        finally
        {
            Directory.Delete(libsPath, recursive: true);
        }
    }

    [Fact]
    public void OnlyConfiguredAssemblies_AreShared()
    {
        var alc = new RemoteHostLoadContext(
            typeof(RemoteHostLoadContextTests).Assembly,
            ["Microsoft.Extensions.Logging.Abstractions"]);

        Assert.True(alc.ShouldShareAssembly("Microsoft.Extensions.Logging.Abstractions"));
        Assert.False(alc.ShouldShareAssembly("Microsoft.Extensions.Http.Resilience"));
        Assert.False(alc.ShouldShareAssembly("StreamJsonRpc"));
        Assert.False(alc.ShouldShareAssembly("Aspire.Hosting"));
    }

    [Fact]
    public void NonSharedAssemblies_NotInLibsDir_AreNotResolved()
    {
        var libsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(libsPath);

        try
        {
            var alc = new RemoteHostLoadContext(typeof(RemoteHostLoadContextTests).Assembly, [], libsPath);

            Assert.Throws<FileNotFoundException>(() => alc.LoadFromAssemblyName(new AssemblyName("Does.Not.Exist")));
        }
        finally
        {
            Directory.Delete(libsPath, recursive: true);
        }
    }

    private static void UnloadAndCollect(WeakReference? reference)
    {
        if (reference is null)
        {
            return;
        }

        for (var i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
