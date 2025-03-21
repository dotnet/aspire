# Aspire.Cli Existing & Proposed Surface

Root command: **aspire**

Global options for debugging:
`-d, --debug`
`-w, --wait-for-debugger`

## Subcommands:

### run (current implementation)

```cli
aspire run [--project <PATH_TO_CSPROJ>]
```
Starts the Aspire app. If no project is specified, it looks in the current directory for a *.csproj. It will error if it can't find a .csproj, or if there are multiple in the directory.

This is a passthrough to `dotnet run` via the CLI runner service.

### new (current implementation)

```cli
aspire new [<TEMPLATE>] [-n|--name <PROJECT_NAME>]
                        [-o|--output <OUTPUT_PATH>]
                        [-v|--version <VERSION>]
                        [-s|--source <NUGET_URL>]
                        [--prerelease]
```

Pulls the latest Aspire templates, then creates a new `aspire-starter` app, unless a specific dotnet template is specified.

Getting the latest templates is a passthrough to `dotnet new install --force` so it always pulls that latest templates even if they are already installed. `--version` is used to specify what version of `Aspire.ProjectTemplates` to pull, defaulting to `9.2.0` currently as a hack or `*-*` if `--prerelease` is specified.

Creating the app is a passthrough to `dotnet new` and propagates the `--name` and `--output` options.

### add / add <PACKAGE_NAME> (current implementation)

```cli
aspire add [<PACKAGE_NAME>] [--project <PATH_TO_CSPROJ>]
                        [-v|--version <VERSION>]
                        [--prerelease]
                        [-s|--source <NUGET_URL>]
```

Adds an Aspire integration if specified, or lists all possible integrations in a selection prompt. Integrations are given friendly names based on the last section of the package id (ie `Aspire.Hosting.Redis` can be referenced as `redis`), with specific subsets having prefixes (az- for `Aspire.Hosting.Azure.*`, aws- for aws, ct- for communitytoolkit).

If no package name is given, it first runs a passthrough to `dotnet package search`. `--prerelease` is propagated. `--source` if specified will limit the search of packages to the specified package feed. If not specified normal NuGet.config rules will be used to search.

If a package is specified, it runs the search but with the specified package as the passed through arg. If there isn't a direct match, it shows the full selection prompt.

Once a package is selected, or if the specified one is found, it runs `dotnet package add` with the fully qualified package ID, and passes through `--version` and `--prerelease` as appropriate.

### build (TODO)