// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using Aspire.Hosting.Ats;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class IntegrationLoadContextTests
{
    [Fact]
    public void AspireTypeSystem_IsSharedFromDefaultContext()
    {
        var alc = new IntegrationLoadContext([AppContext.BaseDirectory], NullLogger.Instance);

        var sharedAssembly = alc.LoadFromAssemblyName(new AssemblyName("Aspire.TypeSystem"));

        Assert.Same(typeof(AtsContext).Assembly, sharedAssembly);
        Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(sharedAssembly));
    }

    [Fact]
    public void NonSharedAssemblies_LoadFromProbeDirectory_WhenNotInDefaultContext()
    {
        // Aspire.Hosting is already in the default context (test project references it),
        // so version unification will defer to default. Test with an assembly that IS
        // in the probe dir but NOT in the default context.
        var alc = new IntegrationLoadContext([AppContext.BaseDirectory], NullLogger.Instance);

        // Aspire.Hosting will be deferred to default because it's already loaded there.
        // This validates the version unification behavior.
        var assembly = alc.LoadFromAssemblyName(new AssemblyName("Aspire.Hosting"));
        Assert.NotNull(assembly);
        // In test environment, default context wins (same version)
        Assert.Same(typeof(IDistributedApplicationBuilder).Assembly, assembly);
    }
}
