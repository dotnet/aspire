// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if false
using Aspire.Workload.Tests;
using Xunit;

public class DotNet_With9_Net8_Fixture : IAsyncLifetime
{
    public string CustomHiveDirectory => TemplatesCustomHive.Net9_0_Net8.IsValueCreated
        ? TemplatesCustomHive.Net9_0_Net8.Value.CustomHiveDirectory
        : throw new InvalidOperationException($"Templates have not been installed {nameof(TemplatesCustomHive.Net9_0_Net8)}");

    public Task InitializeAsync() => TemplatesCustomHive.Net9_0_Net8.Value
                    .InstallAsync(
                        BuildEnvironment.GetNewTemplateCustomHiveDefaultDirectory(),
                        BuildEnvironment.ForDefaultFramework.BuiltNuGetsPath,
                        BuildEnvironment.ForDefaultFramework.DotNet);

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

#endif
