// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

[RequiresDocker("Needs docker to start redis cache")]
public class StarterTemplateWithWithRedisCacheTests : StarterTemplateRunTestsBase<StarterTemplateWithRedisCacheFixture>
{
    protected override int DashboardResourcesWaitTimeoutSecs => 300;

    public StarterTemplateWithWithRedisCacheTests(StarterTemplateWithRedisCacheFixture fixture, ITestOutputHelper testOutput)
        : base(fixture, testOutput)
    {
        HasRedisCache = true;
    }
}
