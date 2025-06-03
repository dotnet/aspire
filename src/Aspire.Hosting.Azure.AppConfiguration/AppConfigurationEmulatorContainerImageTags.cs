// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.AppConfiguration;
internal static class AppConfigurationEmulatorContainerImageTags
{
    // WIP: Official emulator image is still not available in public registry.
    public const string Registry = "docker.io";

    public const string Image = "charlesliangzhy/appconfig-emulator";

    public const string Tag = "0.0.2";
}
