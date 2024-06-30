// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.Garnet;

internal sealed class GarnetContainerImageTags : ICacheContainerImageTags
{
    public const string Registry = "ghcr.io";
    public const string Image = "microsoft/garnet";
    public const string Tag = "1.0";

    public string GetRegistry() => Registry;

    public string GetImage() => Image;

    public string GetTag() => Tag;
}
