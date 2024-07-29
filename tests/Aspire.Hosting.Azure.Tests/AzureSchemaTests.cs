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
        new SchemaTests().ValidateApplicationSamples("CdkResourceWithChildResource", (IDistributedApplicationBuilder builder) =>
        {
            builder.AddPostgres("postgres").PublishAsAzurePostgresFlexibleServer().AddDatabase("db");
        });
    }
}
