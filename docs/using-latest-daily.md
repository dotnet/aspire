# Set up your machine to use the latest Aspire builds

If you just want an official release of .NET Aspire, you don't need this document. [The Aspire documentation](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) will get you started.

If you want the latest, unsupported build of Aspire to try, read on.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Install the daily CLI

On Windows:

```powershell
iex "& { $(irm https://aspire.dev/install.ps1) } -Quality dev"
```

On Linux, or macOS:

```sh
curl -sSL https://aspire.dev/install.sh | bash -s -- -q dev
```

<!-- break between blocks -->

## Create a new Project

Create an empty .NET Aspire project on the command line:

```shell
aspire new
```

Running through the wizard will allow you to select a channel (daily/stable etc).

```shell
Enter the project name (aspire-projects): dailybuild0
Enter the output path: (./dailybuild0): ./dailybuild0
✔  Using Redis Cache for caching.
Select a template version:

   9.4.1 (nuget.org)
>  daily
   stable

(Type to search)
```

When complete, the CLI will create a NuGet.config to make sure that packages are restored from the correct nuget feed.

> [!TIP]
> `aspire new` will automatically update the aspire templates and they will be available in Visual Studio and `dotnet new`.

## Updating an Existing Project

If you have an existing Aspire project and want to update it to use the latest daily build, you can run:

```shell
aspire update
```

This will update the project to use the daily packages and feeds.

> [!TIP]
> `aspire update` can be used at any time to update your project to the latest available Aspire build, including daily builds.

After updating your project, you can run it using the following command:
```shell
aspire run
```
