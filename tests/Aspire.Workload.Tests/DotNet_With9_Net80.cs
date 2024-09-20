// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class DotNet_With9_Net80Fixture : TemplatesCustomHiveFixture
{
    // FIXME: move the package ids to a shared location
    public DotNet_With9_Net80Fixture()
        : base("Aspire.ProjectTemplates.9.0.net8", tempDirName: "templates-with-9-net80")
    {}
}
