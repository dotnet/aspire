// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Secrets;

namespace Aspire.Cli.Tests.Secrets;

public class SecretStoreResolverTests
{
    [Fact]
    public void ComputeSyntheticUserSecretsId_IsDeterministic()
    {
        var path = "/home/user/projects/myapp/apphost.ts";
        var id1 = SecretStoreResolver.ComputeSyntheticUserSecretsId(path);
        var id2 = SecretStoreResolver.ComputeSyntheticUserSecretsId(path);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ComputeSyntheticUserSecretsId_DifferentPaths_ProduceDifferentIds()
    {
        var id1 = SecretStoreResolver.ComputeSyntheticUserSecretsId("/home/user/project1/apphost.ts");
        var id2 = SecretStoreResolver.ComputeSyntheticUserSecretsId("/home/user/project2/apphost.ts");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ComputeSyntheticUserSecretsId_StartsWithAspirePrefix()
    {
        var id = SecretStoreResolver.ComputeSyntheticUserSecretsId("/some/path/apphost.ts");

        Assert.StartsWith("aspire-", id, StringComparison.Ordinal);
    }

    [Fact]
    public void ComputeSyntheticUserSecretsId_IsCaseInsensitive()
    {
        var id1 = SecretStoreResolver.ComputeSyntheticUserSecretsId("/Home/User/Project/apphost.ts");
        var id2 = SecretStoreResolver.ComputeSyntheticUserSecretsId("/home/user/project/apphost.ts");

        Assert.Equal(id1, id2);
    }
}
