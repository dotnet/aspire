// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Storage;
using Azure.ResourceManager.Storage.Models;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureProvisioning();

var sku = builder.AddParameter("storagesku");

var construct1 = builder.AddAzureConstruct("construct1", (construct) =>
{
    var account = construct.AddStorageAccount(
        name: "bob",
        kind: StorageKind.BlobStorage,
        sku: StorageSkuName.StandardLrs
        );

    account.AssignParameter(a => a.Sku.Name, construct.AddParameter(sku));

    account.AddOutput(data => data.PrimaryEndpoints.TableUri, "tableUri", isSecure: true);
});

builder.AddProject<Projects.CdkSample_ApiService>("api")
       .WithEnvironment("TABLE_URI", construct1.GetOutput("tableUri"));

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();

// OPEN QUESTIONS:
// 1. Is it possible to express resourceGroup().location
// 2. Outputting a module and sub-modules (no main.bicep)
// 3. Assigning parameters to mandatory properties (without duplications)
