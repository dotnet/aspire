# Aspire AppHost sample for Azure SignalR Serverless using Emulator

This sample is a simple Aspire AppHost that demonstrates how to use Azure SignalR Serverless with the Azure SignalR Emulator.
Original sample is [here](https://github.com/aspnet/AzureSignalR-samples/tree/main/samples/QuickStartServerless/csharp-isolated)

## How to run
- Set the SignalRServerless.AppHost as the startup project
- Run the project
- Open the Aspire Dashboard and navigate to the client UI of `funcapp` at `/api/index` (e.g: `http://localhost:59079/api/index`). A simple webpage will be displayed to show the current star count of SignalR SDK repo.
