// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Garnet;

internal static class GarnetContainerImageTags
{
    public const string Registry = "ghcr.io";
    public const string Image = "microsoft/garnet";
    //TODO: revert to 1.0 ASAP https://github.com/microsoft/garnet/pull/539 ship as `1.0`
    public const string Tag = "latest";
}
