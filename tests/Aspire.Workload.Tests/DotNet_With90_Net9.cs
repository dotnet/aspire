// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Workload.Tests;

public class DotNet_With9_Net9_Fixture : TemplatesCustomHiveFixture
{
    // FIXME: move the package ids to a shared location
    public DotNet_With9_Net9_Fixture()
        : base(TemplatePackageIds.AspireProjectTemplates_9_0_net9, tempDirName: "templates-with-9-net90")
    {}
}
