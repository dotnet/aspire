// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.KeyVaults;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureProvisioning();

#pragma warning disable CA2252 // This API requires opting into preview features
var sku = builder.AddParameter("storagesku");
var locationOverride = builder.AddParameter("locationOverride");
var signaturesecret = builder.AddParameter("signaturesecret");

var storage = builder.AddAzureStorage("storage", (_, _, account) =>
{
    account.AssignProperty(sa => sa.Sku.Name, sku);
    account.AssignProperty(sa => sa.Location, locationOverride);
});

var blobs = storage.AddBlobs("blobs");

var sqldb = builder.AddSqlServer("sql").AsAzureSqlDatabaseConstruct().AddDatabase("sqldb");

var keyvault = builder.AddAzureKeyVault("mykv", (_, construct, keyVault) =>
{
    var secret = new KeyVaultSecret(construct, name: "mysecret");
    secret.AssignProperty(x => x.Properties.Value, signaturesecret);
});

var cache = builder.AddRedis("cache").AsAzureRedis();
#pragma warning restore CA2252 // This API requires opting into preview features

builder.AddProject<Projects.CdkSample_ApiService>("api")
       .WithReference(blobs)
       .WithReference(sqldb)
       .WithReference(keyvault)
       .WithReference(cache);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
