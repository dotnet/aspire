# Set up your machine to contribute

These instructions will get you ready to contribute to this project. If you just want to use Aspire, see [using-latest-daily.md](using-latest-daily.md).

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Build the repo
`.\build.cmd` (Windows) or `.\build.sh` (macOS and Linux)

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
