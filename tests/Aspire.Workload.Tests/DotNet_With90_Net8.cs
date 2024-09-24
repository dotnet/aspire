// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Workload.Tests;

public class DotNet_With9_Net8_Fixture : TemplatesCustomHiveFixture
{
    // FIXME: move the package ids to a shared location
    public DotNet_With9_Net8_Fixture()
        : base(TemplatePackageIds.AspireProjectTemplates_9_0_net8, tempDirName: "templates-with-9-net80")
    {}
}
