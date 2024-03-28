// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard;

var app = new DashboardWebApplication();
if (app.ValidationFailures.Count > 0)
{
    return -1;
}

app.Run();
return 0;
