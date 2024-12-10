# Aspire AppHost sample for Azure SignalR Serverless using Emulator

This sample is an Aspire AppHost project that demonstrates how to use Azure SignalR Serverless with the Azure SignalR Emulator.
Original sample can be found [here](https://github.com/aspnet/AzureSignalR-samples/tree/main/samples/DotnetIsolated-BidirectionChat)

## Requirements
- Please make sure you match the project constraints for Aspire Azure Functions [here](https://learn.microsoft.com/dotnet/aspire/serverless/functions#azure-function-project-constraints)

## How to run
- Set the SignalRServerless.AppHost as the startup project
- Run the project
- Open the Aspire Dashboard and navigate to the client UI of `funcapp` at `/api/index` (e.g: `http://localhost:7071/api/index`). A chat sample website will show up, and you can send messages by entering them into the chat box
