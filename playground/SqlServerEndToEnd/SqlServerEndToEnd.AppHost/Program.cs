// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

var sql1 = builder.AddAzureSqlServer("sql1")
        .ConfigureInfrastructure(c =>
        {
            const string FREE_DB_SKU = "GP_S_Gen5_2";

            foreach (var database in c.GetProvisionableResources().OfType<SqlDatabase>())
            {
                database.Sku = new SqlSku() { Name = FREE_DB_SKU };
            }
        });

//.RunAsContainer();

var db1 = sql1.AddDatabase("db1");

var sql2 = builder.AddAzureSqlServer("sql2")
        .ConfigureInfrastructure(c =>
        {
            const string FREE_DB_SKU = "GP_S_Gen5_2";

            foreach (var database in c.GetProvisionableResources().OfType<SqlDatabase>())
            {
                database.Sku = new SqlSku() { Name = FREE_DB_SKU };
            }
        });

var db2 = sql2.AddDatabase("db2");

var dbsetup = builder.AddProject<Projects.SqlServerEndToEnd_DbSetup>("dbsetup")
                     .WithReference(db1).WaitFor(sql1)
                     .WithReference(db2).WaitFor(sql2);

builder.AddProject<Projects.SqlServerEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(db1).WaitFor(db1)
       .WithReference(db2).WaitFor(db2)
       .WaitForCompletion(dbsetup);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
