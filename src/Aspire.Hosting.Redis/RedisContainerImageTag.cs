// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.Redis;

internal sealed class RedisContainerImageTag() : CacheContainerImageTags(Registry, Image, Tag)
{
    public const string Registry = "docker.io";
    public const string Image = "library/redis";
    public const string Tag = "7.2";
    public const string RedisCommanderRegistry = "docker.io";
    public const string RedisCommanderImage = "rediscommander/redis-commander";
    public const string RedisCommanderTag = "latest";
}
