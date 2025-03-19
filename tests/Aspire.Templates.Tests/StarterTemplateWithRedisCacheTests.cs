// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Xunit.Abstractions;

namespace Aspire.Templates.Tests;

[RequiresDocker("Needs docker to start redis cache")]
[RequiresSSLCertificate]
public class StarterTemplateWithRedisCacheTests : StarterTemplateRunTestsBase<StarterTemplateWithRedisCacheFixture>
{
    protected override int DashboardResourcesWaitTimeoutSecs => 300;

    public StarterTemplateWithRedisCacheTests(StarterTemplateWithRedisCacheFixture fixture, ITestOutputHelper testOutput)
        : base(fixture, testOutput)
    {
        HasRedisCache = true;
    }
}
