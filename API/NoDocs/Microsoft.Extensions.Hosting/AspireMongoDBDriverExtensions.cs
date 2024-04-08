// Assembly 'Aspire.MongoDB.Driver'

using System;
using Aspire.MongoDB.Driver;
using MongoDB.Driver;

namespace Microsoft.Extensions.Hosting;

public static class AspireMongoDBDriverExtensions
{
    public static void AddMongoDBClient(this IHostApplicationBuilder builder, string connectionName, Action<MongoDBSettings>? configureSettings = null, Action<MongoClientSettings>? configureClientSettings = null);
    public static void AddKeyedMongoDBClient(this IHostApplicationBuilder builder, string name, Action<MongoDBSettings>? configureSettings = null, Action<MongoClientSettings>? configureClientSettings = null);
}
