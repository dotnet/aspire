# Set up your machine to contribute

These instructions will get you ready to contribute to this project. If you just want to use Aspire, see [using-latest-daily.md](using-latest-daily.md).

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Build the repo
`.\build.cmd` (Windows) or `.\build.sh` (macOS and Linux)

## Enable Azure ServiceBus (optional)

1. Follow [these instructions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#create-a-namespace-in-the-azure-portal) to create a ServiceBus Namespace (pick a unique namespace) and a Queue (explicitly name the Queue "orders").
    - Be sure to follow the "Assign roles to your Azure AD user" and "Launch Visual Studio and sign-in to Azure" sections so the app can authenticate as you.
2. Add the following user-secret to the `samples\eShopLite\AppHost` orchestrator project (using the unique namespace you created above):

```shell
# in the eshopLite/AppHost folder
dotnet user-secrets set ConnectionStrings:messaging <serviceBusNamespace>.servicebus.windows.net
```

If you use Visual Studio you can do the same thing by right-clicking `AppHost` in the Solution Explorer then choosing `Manage User Secrets` and adding

```json
{
  "ConnectionStrings": {
    "messaging": "<serviceBusNamespace>.servicebus.windows.net"
  }
}
```

The `<ServiceBus namespace host>` is labeled in the portal UI as "Host name" e.g. myservicebusinstance.servicebus.windows.net

## Run eShopLite

This will confirm that you're all set up.

If you are using Visual Studio:

1. Open `Aspire.sln`
1. Set the Startup Project to be the `AppHost` project (it's under `\samples\eShopLite`). Make sure the launch profile is set to "http".
1. <kbd>F5</kbd> to debug, or <kbd>Ctrl+F5</kbd> to launch without debugging.

Otherwise:
```shell
dotnet restore samples/eShopLite/AppHost/AppHost.csproj
dotnet run --project samples/eShopLite/AppHost/AppHost.csproj
```

## View Dashboard

When you start the sample app in Visual Studio, it will automatically open your browser to show the dashboard.

Otherwise if you are using the command line, when you have the Aspire app running, open the dashboard URL in your browser. The URL is shown in the app's console output like this: `Now listening on: http://localhost:15888`. You can change the default URL in the launchSettings.json file in the AppHost project.

## Tips and known issues

Make sure you have started Docker before trying to run an Aspire app.

See the [tips and known issues](tips-and-known-issues.md) page.
