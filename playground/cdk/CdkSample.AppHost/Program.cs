// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.KeyVaults;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureProvisioning();

var cosmosdb = builder.AddAzureCosmosDBConstruct("cosmos").AddDatabase("cosmosdb");

var sku = builder.AddParameter("storagesku");
var locationOverride = builder.AddParameter("locationOverride");
var storage = builder.AddAzureConstructStorage("storage", (_, account) =>
{
    account.AssignProperty(sa => sa.Sku.Name, sku);
    account.AssignProperty(sa => sa.Location, locationOverride);
});

var blobs = storage.AddBlobs("blobs");

var sqldb = builder.AddSqlServer("sql").AsAzureSqlDatabaseConstruct().AddDatabase("sqldb");

var signaturesecret = builder.AddParameter("signaturesecret");
var keyvault = builder.AddAzureKeyVaultConstruct("mykv", (construct, keyVault) =>
{
    var secret = new KeyVaultSecret(construct, name: "mysecret");
    secret.AssignProperty(x => x.Properties.Value, signaturesecret);
});

var cache = builder.AddRedis("cache").AsAzureRedisConstruct();

var pgsqlAdministratorLogin = builder.AddParameter("pgsqlAdministratorLogin");
var pgsqlAdministratorLoginPassword = builder.AddParameter("pgsqlAdministratorLoginPassword", secret: true);
var pgsqldb = builder.AddPostgres("pgsql")
                   .AsAzurePostgresFlexibleServerConstruct(pgsqlAdministratorLogin, pgsqlAdministratorLoginPassword)
                   .AddDatabase("pgsqldb");

var pgsql2 = builder.AddPostgres("pgsql2").AsAzurePostgresFlexibleServerConstruct();

var mongo = builder.AddMongoDB("mongo").PublishAsAzureCosmosDB();

builder.AddProject<Projects.CdkSample_ApiService>("api")
       .WithReference(blobs)
       .WithReference(sqldb)
       .WithReference(keyvault)
       .WithReference(cache)
       .WithReference(cosmosdb)
       .WithReference(pgsqldb);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
