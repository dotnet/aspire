// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureBicepResourceTests
{
    [Fact]
    public void AddBicepResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("mytemplate", "content")
                                   .WithParameter("param1", "value1")
                                   .WithParameter("param2", "value2");

        Assert.Equal("content", bicepResource.Resource.TemplateString);
        Assert.Equal("value1", bicepResource.Resource.Parameters["param1"]);
        Assert.Equal("value2", bicepResource.Resource.Parameters["param2"]);
    }

    [Fact]
    public void GetOutputReturnsOutputValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.Outputs["resourceEndpoint"] = "https://myendpoint";

        Assert.Equal("https://myendpoint", bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetOutputValueThrowsIfNoOutput()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void AssertManifestLayout()
    {
        var builder = DistributedApplication.CreateBuilder();

        var param = builder.AddParameter("p1");

        var bicepResource = builder.AddBicepTemplateString("templ", "content")
                                    .WithParameter("param1", "value1")
                                    .WithParameter("param2", ["1", "2"])
                                    .WithParameter("param3", new JsonObject() { ["value"] = "nested" })
                                    .WithParameter("param4", param);

        // This makes a temp file
        var obj = GetManifest(bicepResource.Resource.WriteToManifest);

        Assert.NotNull(obj);
        Assert.Equal("azure.bicep.v0", obj["type"]?.ToString());
        Assert.NotNull(obj["path"]?.ToString());
        var parameters = obj["params"];
        Assert.NotNull(parameters);
        Assert.Equal("value1", parameters?["param1"]?.ToString());
        Assert.Equal("1", parameters?["param2"]?[0]?.ToString());
        Assert.Equal("2", parameters?["param2"]?[1]?.ToString());
        Assert.Equal("nested", parameters?["param3"]?["value"]?.ToString());
        Assert.Equal($"{{{param.Resource.Name}.value}}", parameters?["param4"]?.ToString());
    }

    [Fact]
    public void AddBicepCosmosDb()
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddBicepCosmosDb("cosmos");
        cosmos.AddDatabase("db", "mydatabase");

        cosmos.Resource.SecretOutputs["connectionString"] = "mycosmosconnectionstring";

        var databases = cosmos.Resource.Parameters["databases"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.cosmosdb.bicep", cosmos.Resource.TemplateResourceName);
        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("cosmos", cosmos.Resource.Parameters["databaseAccountName"]);
        Assert.NotNull(databases);
        Assert.Equal(["mydatabase"], databases);
        Assert.Equal("mycosmosconnectionstring", cosmos.Resource.GetConnectionString());
        Assert.Equal("{cosmos.secretOutputs.connectionString}", cosmos.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void AddBicepCosmosDbDatabaseReferencesParentConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddBicepCosmosDb("cosmos").AddDatabase("db", "mydatabase");

        var obj = GetManifest(db.Resource.WriteToManifest);

        Assert.NotNull(obj);
        Assert.Equal("azure.bicep.v0", obj["type"]?.ToString());
        Assert.Equal("{cosmos.connectionString}", obj["connectionString"]?.ToString());
        Assert.Equal("cosmos", obj["parent"]?.ToString());
    }

    [Fact]
    public void AddAppConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddBicepAppConfiguration("appConfig");

        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.appconfig.bicep", appConfig.Resource.TemplateResourceName);
        Assert.Equal("appConfig", appConfig.Resource.Name);
        Assert.Equal("appconfig", appConfig.Resource.Parameters["configName"]);
        Assert.Equal("https://myendpoint", appConfig.Resource.GetConnectionString());
        Assert.Equal("{appConfig.outputs.appConfigEndpoint}", appConfig.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void AddBicepRedis()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddBicepAzureRedis("redis");

        redis.Resource.SecretOutputs["connectionString"] = "myconnectionstring";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.redis.bicep", redis.Resource.TemplateResourceName);
        Assert.Equal("redis", redis.Resource.Name);
        Assert.Equal("redis", redis.Resource.Parameters["redisCacheName"]);
        Assert.Equal("myconnectionstring", redis.Resource.GetConnectionString());
        Assert.Equal("{redis.secretOutputs.connectionString}", redis.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void PublishAsRedisPublishesRedisAsAzureRedis()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("cache").PublishAsAzureRedis();

        Assert.True(redis.Resource.IsContainer());

        var manifestCallback = redis.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().Single();

        Assert.NotNull(manifestCallback?.Callback);

        var manifest = GetManifest(manifestCallback.Callback);

        Assert.Equal("azure.bicep.v0", manifest["type"]?.ToString());
        Assert.Equal("{cache.secretOutputs.connectionString}", manifest["connectionString"]?.ToString());
    }

    [Fact]
    public void AddBicepKeyVault()
    {
        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder.AddBicepKeyVault("keyVault");

        keyVault.Resource.Outputs["vaultUri"] = "https://myvault";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.keyvault.bicep", keyVault.Resource.TemplateResourceName);
        Assert.Equal("keyVault", keyVault.Resource.Name);
        Assert.Equal("keyvault", keyVault.Resource.Parameters["vaultName"]);
        Assert.Equal("https://myvault", keyVault.Resource.GetConnectionString());
        Assert.Equal("{keyVault.outputs.vaultUri}", keyVault.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void AddBicepSqlServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddBicepAzureSqlServer("sql");
        sql.AddDatabase("db", "database");

        sql.Resource.Outputs["sqlServerFqdn"] = "myserver";

        var databases = sql.Resource.Parameters["databases"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.sql.bicep", sql.Resource.TemplateResourceName);
        Assert.Equal("sql", sql.Resource.Name);
        Assert.Equal("sql", sql.Resource.Parameters["serverName"]);
        Assert.NotNull(databases);
        Assert.Equal(["database"], databases);
        Assert.Equal("Server=tcp:myserver,1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.GetConnectionString());
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void AddBicepServiceBus()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sb = builder.AddBicepAzureServiceBus("sb", ["queue1"], ["topic1"]);

        sb.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        var queues = sb.Resource.Parameters["queues"] as IEnumerable<string>;
        var topics = sb.Resource.Parameters["topics"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.servicebus.bicep", sb.Resource.TemplateResourceName);
        Assert.Equal("sb", sb.Resource.Name);
        Assert.Equal("sb", sb.Resource.Parameters["serviceBusNamespaceName"]);
        Assert.NotNull(queues);
        Assert.Equal(["queue1"], queues);
        Assert.NotNull(topics);
        Assert.Equal(["topic1"], topics);
        Assert.Equal("mynamespaceEndpoint", sb.Resource.GetConnectionString());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", sb.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void AddBicepStorage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var storage = builder.AddAzureBicepAzureStorage("storage");

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        var blob = storage.AddBlob("blob");
        var queue = storage.AddQueue("queue");
        var table = storage.AddTable("table");

        Assert.Equal("Aspire.Hosting.Azure.Bicep.storage.bicep", storage.Resource.TemplateResourceName);
        Assert.Equal("storage", storage.Resource.Name);
        Assert.Equal("storage", storage.Resource.Parameters["storageName"]);

        Assert.Equal("https://myblob", blob.Resource.GetConnectionString());
        Assert.Equal("https://myqueue", queue.Resource.GetConnectionString());
        Assert.Equal("https://mytable", table.Resource.GetConnectionString());
        Assert.Equal("{storage.outputs.blobEndpoint}", blob.Resource.ConnectionStringExpression);
        Assert.Equal("{storage.outputs.queueEndpoint}", queue.Resource.ConnectionStringExpression);
        Assert.Equal("{storage.outputs.tableEndpoint}", table.Resource.ConnectionStringExpression);

        var blobManifest = GetManifest(blob.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.blobEndpoint}", blobManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());

        var queueManifest = GetManifest(queue.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.queueEndpoint}", queueManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());

        var tableManifest = GetManifest(table.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.tableEndpoint}", tableManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());
    }

    private static JsonNode GetManifest(Action<ManifestPublishingContext> writeManifest)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        writeManifest(new ManifestPublishingContext(executionContext, Environment.CurrentDirectory, writer));
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        return obj;
    }
}
