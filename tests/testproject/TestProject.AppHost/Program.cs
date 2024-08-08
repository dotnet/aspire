// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var testProgram = TestProgram.Create<Program>(args, includeIntegrationServices: true, disableDashboard: false);

// Run a task to read from the console and stop the app if an external process sends "Stop".
// This allows for easier control than sending CTRL+C to the console in a cross-platform way.
_ = Task.Run(async () =>
{
    var s = Console.ReadLine();
    if (s == "Stop")
    {
        if (testProgram.App is not null)
        {
            await testProgram.App.StopAsync();
        }
    }
});

await testProgram.RunAsync();
