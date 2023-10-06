// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

public class RedisComponent(string name, string? connectionString) : DistributedApplicationComponent(name), IRedisComponent
{
    public string? GetConnectionString() => connectionString;
}
