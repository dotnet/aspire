# Set up your environment

Follow all steps in [machine-requirements](machine-requirements.md).

# Building the repo

You can build the repo from the command line by simply `.\build.cmd`.

You can launch VS with:

1. `.\restore.cmd`
2. `.\startvs.cmd`

# Run the .NET Aspire eShopLite sample

## Enable Azure ServiceBus (optional)

1. Follow [these instructions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#create-a-namespace-in-the-azure-portal) to create a ServiceBus Namespace (pick a unique namespace) and a Queue (explicitly name the Queue "orders").
    - Be sure to follow the "Assign roles to your Azure AD user" and "Launch Visual Studio and sign-in to Azure" sections so the app can authenticate as you.
2. Add the following user-secret to the `samples\eShopLite\AppHost` orchestrator project (using the unique namespace you created above):

```shell
C:\git\aspire\samples\eShopLite\AppHost> dotnet user-secrets set ConnectionStrings:messaging <serviceBusNamespace>.servicebus.windows.net
```

- You can do the same in VS by right-clicking AppHost in the Solution Explorer -> "Manage User Secrets" and add

```json
{
  "ConnectionStrings": {
    "messaging": "<serviceBusNamespace>.servicebus.windows.net"
  }
}
```

- The `<ServiceBus namespace host>` is labeled in the portal UI as "Host name" e.g. myservicebusinstance.servicebus.windows.net

## Load the Sample Application

1. Make sure Docker Desktop is running
2. Open `Aspire.sln` solution
3. Set `AppHost` project under `\samples\eShopLite` folder to be the startup project. Make sure the launch profile is set to "http".
4. <kbd>F5</kbd> and enjoy.
5. When you are done, "Stop Debugging".

# Dashboard

Starting debugging in VS will automatically launch the browser with dashboard which is being served at URL "http://localhost:15888" by default. The URL is controlled by launchSettings.json file in AppHost project.

# Tips and known issues

See the [tips and known issues](tips-and-known-issues.md) page.
