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

These will create a `.slnx` file and at least two projects.

Assuming the NuGet feed you added above is visible -- for example you added it globally or it's in a NuGet.config in this folder - you can now run it (make sure that Docker desktop is started):

```shell
aspire run
```

> [!TIP]
> If you see an error attempting to run the application with aspire run, it's likely that you need to update the Aspire packages in the application. You can always use `dotnet run` on the *.AppHost project as a fallback (please report an issue before you do so!)
