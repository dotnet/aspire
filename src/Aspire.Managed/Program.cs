// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard;

return args switch
{
    ["dashboard", .. var rest] => RunDashboard(rest),
    ["server", .. var rest] => await RunServer(rest).ConfigureAwait(false),
    ["nuget", .. var rest] => await RunNuGet(rest).ConfigureAwait(false),
    _ => ShowUsage()
};

static int RunDashboard(string[] args)
{
    var options = new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    };

    var app = new DashboardWebApplication(options: options);
    return app.Run();
}

static async Task<int> RunServer(string[] args)
{
    await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args).ConfigureAwait(false);
    return 0;
}

static async Task<int> RunNuGet(string[] args)
{
    return await Aspire.Cli.NuGetHelper.Program.Main(args).ConfigureAwait(false);
}

static int ShowUsage()
{
    Console.Error.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} <dashboard|server|nuget> [args...]");
    return 1;
}
