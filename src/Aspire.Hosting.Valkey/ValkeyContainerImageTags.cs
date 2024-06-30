// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.Valkey;

internal sealed class ValkeyContainerImageTags : ICacheContainerImageTags
{
    public const string Registry = "valkey";
    public const string Image = "valkey";
    public const string Tag = "7.2";

    public string GetRegistry() => Registry;

    public string GetImage() => Image;

    public string GetTag() => Tag;
}
