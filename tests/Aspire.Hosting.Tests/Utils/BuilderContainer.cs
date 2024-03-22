// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

public sealed class BuilderContainer : IDisposable
{
    public IDistributedApplicationBuilder Builder { get; }

    public static BuilderContainer Create() => new BuilderContainer(DistributedApplication.CreateBuilder());

    private BuilderContainer(IDistributedApplicationBuilder builder)
    {
        Builder = builder;
    }

    public void Dispose()
    {
        try
        {
            Builder.Build().Dispose();
        }
        catch
        {
            // Ignore errors.
        }
    }
}

