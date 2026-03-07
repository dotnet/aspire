// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class IntegrationLoadContextTests
{
    [Fact]
    public void FrameworkTypes_NotInLibsDir_FallBackToDefault()
    {
        // Arrange — point at an empty directory so nothing is found in libs
        var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(emptyDir);

        try
        {
            var alc = new IntegrationLoadContext(emptyDir);

            // Act — loading a framework assembly falls back to Default since it's not in libs
            var asm = alc.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));

            // Assert — should be the same as Default (resolved via fallback)
            var defaultAsm = AssemblyLoadContext.Default.LoadFromAssemblyName(
                new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));
            Assert.Same(defaultAsm, asm);
        }
        finally
        {
            Directory.Delete(emptyDir, true);
        }
    }

    [Fact]
    public void AspireHosting_LoadsFromLibsDir_WhenPresent()
    {
        // Arrange — the test output dir has Aspire.Hosting.dll
        var libsPath = AppContext.BaseDirectory;
        var alc = new IntegrationLoadContext(libsPath);

        // Act — load Aspire.Hosting through the integration context
        var asm = alc.LoadFromAssemblyName(new AssemblyName("Aspire.Hosting"));

        // Assert — Aspire.Hosting is isolated in the integration context
        var defaultAsm = typeof(Aspire.Hosting.Ats.ICodeGenerator).Assembly;
        Assert.NotSame(defaultAsm, asm);
        Assert.Equal("IntegrationLibs", AssemblyLoadContext.GetLoadContext(asm)?.Name);
    }

    [Fact]
    public void NonSharedAssemblies_LoadFromLibsDir_InIntegrationContext()
    {
        var libsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(libsPath);
        IntegrationLoadContext? alc = null;
        WeakReference? alcRef = null;

        try
        {
            var sourceAssemblyPath = typeof(IntegrationLoadContextTests).Assembly.Location;
            var destinationAssemblyPath = Path.Combine(libsPath, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath);

            alc = new IntegrationLoadContext(libsPath);
            alcRef = new WeakReference(alc);

            var isolatedAsm = alc.LoadFromAssemblyName(new AssemblyName(typeof(IntegrationLoadContextTests).Assembly.GetName().Name!));

            Assert.NotSame(typeof(IntegrationLoadContextTests).Assembly, isolatedAsm);
            Assert.Equal("IntegrationLibs", AssemblyLoadContext.GetLoadContext(isolatedAsm)?.Name);
        }
        finally
        {
            if (alc is { })
            {
                alc.Unload();
            }

            if (alcRef is not null)
            {
                for (var i = 0; i < 10 && alcRef.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            if (alcRef is null || !alcRef.IsAlive)
            {
                Directory.Delete(libsPath, recursive: true);
            }
        }
    }

    [Fact]
    public void FrameworkAssemblies_AreShared()
    {
        Assert.True(IntegrationLoadContext.ShouldShareAssembly("System.Private.CoreLib"));
        Assert.True(IntegrationLoadContext.ShouldShareAssembly("System.Diagnostics.DiagnosticSource"));
        Assert.True(IntegrationLoadContext.ShouldShareAssembly("System.Diagnostics.EventLog"));
        Assert.True(IntegrationLoadContext.ShouldShareAssembly("StreamJsonRpc"));
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

            var alc = new IntegrationLoadContext(libsPath);

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
    public void SystemPrefixedIntegrationAssemblies_AreNotShared()
    {
        Assert.False(IntegrationLoadContext.ShouldShareAssembly("System.ClientModel"));
        Assert.False(IntegrationLoadContext.ShouldShareAssembly("Microsoft.Extensions.Http.Resilience"));
    }
}
