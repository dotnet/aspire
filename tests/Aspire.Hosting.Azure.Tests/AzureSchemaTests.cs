// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Schema;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSchemaTests
{
    [Fact]
    public void ValidateApplicationSamples()
    {
        var schemaTests = new SchemaTests();

        schemaTests.ValidateApplicationSamples("CdkResourceWithChildResource", (IDistributedApplicationBuilder builder) =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            builder.AddPostgres("postgres").PublishAsAzurePostgresFlexibleServer().AddDatabase("db");
#pragma warning restore CS0618 // Type or member is obsolete
        });

        schemaTests.ValidateApplicationSamples("CdkResourceWithChildResource", (IDistributedApplicationBuilder builder) =>
        {
            builder.AddAzurePostgresFlexibleServer("postgres").AddDatabase("db");
        });
    }
}
