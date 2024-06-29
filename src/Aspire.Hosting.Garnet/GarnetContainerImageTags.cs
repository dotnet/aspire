// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.Garnet;

internal sealed class GarnetContainerImageTags() : CacheContainerImageTags(RegistryValue, ImageValue, TagValue)
{
    private const string RegistryValue = "ghcr.io";
    private const string ImageValue = "microsoft/garnet";
    private const string TagValue = "1.0";
}
