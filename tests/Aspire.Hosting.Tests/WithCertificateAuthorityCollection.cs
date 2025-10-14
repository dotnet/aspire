// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class WithCertificateAuthorityCollectionTests
{
    [Fact]
    public async Task AddingCertificateAuthorityCollectionsIsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bundle1 = builder.AddCertificateAuthorityCollection("bundle1");
        var bundle2 = builder.AddCertificateAuthorityCollection("bundle2");

        var container = builder.AddContainer("container", "image")
                               .WithEnvironment(context =>
                               {
                                   Assert.NotNull(context.Resource);

                                   var sp = context.ExecutionContext.ServiceProvider;
                                   context.EnvironmentVariables["SP_AVAILABLE"] = sp is not null ? "true" : "false";
                               })
                               .WithCertificateAuthorityCollection(bundle1)
                               .WithCertificateAuthorityCollection(bundle2)
                               .WithCertificateAuthorityCollection(bundle1); // Add bundle1 again to test idempotency

        Assert.True(container.Resource.TryGetAnnotationsOfType<CertificateAuthorityCollectionAnnotation>(out var annotation));
        Assert.Equal(2, annotation.CertificateAuthorityCollections.Count);
        Assert.Contains(bundle1.Resource, annotation.CertificateAuthorityCollections);
        Assert.Contains(bundle2.Resource, annotation.CertificateAuthorityCollections);
    }
}