// This playground demonstrates the container stdin support feature.
// It creates containers that can receive stdin input, showcasing the WithStdin() extension method.
//
// When WithStdin() is called:
// 1. The container is started with the -i flag (interactive mode)
// 2. A "Send Input" command is automatically added to the resource
// 3. Users can click "Send Input" in the dashboard to send text to the container's stdin
//
// To test stdin functionality:
// 1. Run this playground: dotnet run
// 2. Open the Aspire Dashboard (URL shown in console)
// 3. Navigate to a container resource (e.g., stdin-echo)
// 4. Click "Send Input" command button
// 5. Enter text and click OK
// 6. View the container's console logs to see the response

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.StdioEndToEnd_StdinEcho>("stdin-echo-dotnet")
    .WithStdin();  // Enable stdin input command for the project resource

// Create a simple container that reads from stdin and echoes each line back with a "Received: " prefix.
// The 'alpine' image with a shell script will read stdin and write to stdout.
_ = builder.AddContainer("stdin-echo-container", "alpine")
    .WithArgs("sh", "-c", "echo 'Container started. Waiting for stdin input...' && while read line; do echo \"Received: $line\"; done")
    .WithStdin();  // Enable stdin support - container will be started with -i flag

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
