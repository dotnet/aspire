// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

public class RedisResource(string name, string? connectionString) : DistributedApplicationResource(name), IRedisResource
{
    public string? GetConnectionString() => connectionString;
}
