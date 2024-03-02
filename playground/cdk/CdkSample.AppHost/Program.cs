// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Storage;
using Azure.ResourceManager.Storage.Models;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureProvisioning();

var sku = builder.AddParameter("storagesku");
var locationOverride = builder.AddParameter("locationOverride");

var cdkstorage1 = builder.AddAzureConstruct("cdkstorage1", (construct) =>
{
    var account = construct.AddStorageAccount(
        name: "cdkstorage1",
        kind: StorageKind.Storage,
        sku: StorageSkuName.StandardLrs
        );
    account.AssignParameter(a => a.Sku.Name, sku);

    account.AddOutput(data => data.PrimaryEndpoints.TableUri, "tableUri", isSecure: true);
});

var cdkstorage2 = builder.AddAzureConstructStorage("cdkstorage2", (_, account) =>
{
    account.AssignParameter(sa => sa.Sku.Name, sku);
    account.AssignParameter(sa => sa.Location, locationOverride);
});

var blobs = cdkstorage2.AddBlobs("blobs");

builder.AddProject<Projects.CdkSample_ApiService>("api")
       .WithReference(blobs);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
