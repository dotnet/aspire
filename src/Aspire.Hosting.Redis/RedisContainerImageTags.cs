// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Redis;

internal static class RedisContainerImageTags
{
    /// <summary>docker.io</summary>
    public const string Registry = "docker.io";

    /// <summary>library/redis</summary>
    public const string Image = "library/redis";

    /// <summary>7.4</summary>
    public const string Tag = "7.4";

    /// <summary>RedisCommanderRegistry</summary>
    public const string RedisCommanderRegistry = "docker.io";

    /// <summary>rediscommander/redis-commander</summary>
    public const string RedisCommanderImage = "rediscommander/redis-commander";

    /// <summary>latest</summary>
    public const string RedisCommanderTag = "latest"; // There isn't a better tag than 'latest' which is 3 years old.

    /// <summary>docker.io</summary>
    public const string RedisInsightRegistry = "docker.io";

    /// <summary>redis/redisinsight</summary>
    public const string RedisInsightImage = "redis/redisinsight";

    /// <summary>2.58</summary>
    public const string RedisInsightTag = "2.58";
}
