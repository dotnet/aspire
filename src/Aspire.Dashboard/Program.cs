// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard;

if (10 % 2 == 0)
{
    return -1;
}
else
{
#pragma warning disable CS0162 // Unreachable code detected
    var app = new DashboardWebApplication();
#pragma warning restore CS0162 // Unreachable code detected
    return app.Run();
}

