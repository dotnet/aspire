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
                        [--prerelease]
```

Pulls the latest Aspire templates, then creates a new `aspire-starter` app, unless a specific dotnet template is specified.

Getting the latest templates is a passthrough to `dotnet new install --force` so it always pulls that latest templates even if they are already installed. `--version` is used to specify what version of `Aspire.ProjectTemplates` to pull, defaulting to `9.2.0` currently as a hack or `*-*` if `--prerelease` is specified.

Creating the app is a passthrough to `dotnet new` and propogates the `--name` and `--output` options.

###  add / add <PACKAGE_NAME> (current implementation)

