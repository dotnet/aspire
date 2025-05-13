# Set up your machine to contribute

These instructions will get you ready to contribute to this project. If you just want to use Aspire, see [using-latest-daily.md](using-latest-daily.md).

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Build the repo
`.\build.cmd` (Windows) or `.\build.sh` (macOS and Linux)

## Run TestShop

This will confirm that you're all set up.

If you are using Visual Studio:

1. Open `Aspire.slnx`
1. Set the Startup Project to be the `AppHost` project (it's under `\playground\TestShop`). Make sure the launch profile is set to "http".
1. <kbd>F5</kbd> to debug, or <kbd>Ctrl+F5</kbd> to launch without debugging.

Otherwise:
```shell
dotnet restore playground/TestShop/TestShop.AppHost/TestShop.AppHost.csproj
dotnet run --project playground/TestShop/TestShop.AppHost/TestShop.AppHost.csproj
```

## View Dashboard

When you start the sample app in Visual Studio, it will automatically open your browser to show the dashboard.

Otherwise if you are using the command line, when you have the Aspire app running, open the dashboard URL in your browser. The URL is shown in the app's console output like this: `Now listening on: http://localhost:15888`. You can change the default URL in the launchSettings.json file in the AppHost project.

## Localization

If you are contributing to Aspire.Dashboard, please ensure that all strings are localized. If necessary,
create a new resx file under `Aspire.Dashboard\Resources`. To reference a string, ensure the `IStringLocalizer` for the resx file is
injected. An example is below:

```xml
@inject IStringLocalizer<Resources.ResxFile> Loc
...
<p>@Loc[Resources.ResxFile.YourStringHere]</p>
```

Note that injection doesn't happen until a component's `OnInitialized`, so if you are referencing a string from codebehind, you must wait to do that
until `OnInitialized`.

## Integrations (Formerly Components)

Please check the [.NET Aspire integrations contribution guidelines](../src/Components/README.md) if you intend to make contributions to a new or existing .NET Aspire integration.

## Generating local NuGet packages

If you want to try local changes on a separate Aspire based project or solution it can be useful to generate the NuGet packages
in a local folder and use it as a package source.

To do so simply execute:
`.\build.cmd -pack` (Windows) or `.\build.sh -pack` (macOS and Linux)

This will generate all the packages in the folder `./artifacts/packages/Debug/Shipping`. At this point from your solution folder run:

`dotnet nuget add source my_aspire_folder/artifacts/packages/Debug/Shipping`

Or edit the `NuGet.config` file and add this line to the `<packageSources>` list:

```xml
<add key="aspire-dev" value="my_aspire_folder/artifacts/packages/Debug/Shipping" />
```

## Tips and known issues

Make sure you have started Docker before trying to run an Aspire app.

For information on who can help in PRs and issues, see the [area owners](area-owners.md) page.

See the [tips and known issues](tips-and-known-issues.md) page.

### Package validation

When creating a new integration, package validation will automatically try to download a previous version of the package to ensure you didn't break compat. As a result you might get the following build error:

```shell
error NU1101: Unable to find package [NEW PACKAGE NAME]. No packages exist with this id in source(s): dotnet-eng, dotnet-public, dotnet9, dotnet9-transport. PackageSourceMapping is enabled, the following source(s) were not considered: dotnet-libraries.
```

To prevent this the new package needs this line to be added to the `.csproj`:

```xml
<EnablePackageValidation>false</EnablePackageValidation>
```
