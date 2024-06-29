// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.Garnet;

internal sealed class GarnetContainerImageTags() : CacheContainerImageTags(Registry, Image, Tag)
{
    public const string Registry = "ghcr.io";
    public const string Image = "microsoft/garnet";
    public const string Tag = "1.0";
}
