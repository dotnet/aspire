// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Kubernetes.Tests;

public class KubernetesPublisherFixture : IDisposable
{
    public const string CollectionName = "Kubernetes Publisher Collection";

    public TempDirectory? TempDirectoryInstance { get; } = new();

    public void Dispose()
    {
        TempDirectoryInstance?.Dispose();
        GC.SuppressFinalize(this);
    }

    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(".aspire-kubernetes").FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

[CollectionDefinition(KubernetesPublisherFixture.CollectionName)]
public class DatabaseCollection : ICollectionFixture<KubernetesPublisherFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
