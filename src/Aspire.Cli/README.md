# Aspire CLI Command Reference

The Aspire CLI is used to create, run, and publish Aspire-based applications.

## Usage

```
aspire [command] [options]
```

## Global Options

- `-d, --debug` - Enable debug logging to the console
- `--wait-for-debugger` - Wait for a debugger to attach before executing the command
- `-?, -h, --help` - Show help and usage information
- `--version` - Show version information

## Commands

### run

Run an Aspire app host in development mode.

```cli
aspire run [options] [[--] <additional arguments>...]
```

**Options:**
- `--project` - The path to the Aspire app host project file
- `-w, --watch` - Start project resources in watch mode

**Additional Arguments:**
Arguments passed to the application that is being run.

**Description:**
Starts the Aspire app host. If no project is specified, it looks in the current directory for a *.csproj file. It will error if it can't find a .csproj, or if there are multiple in the directory.

### new

Create a new Aspire project.

```cli
aspire new [command] [options]
```

**Options:**
- `-n, --name` - The name of the project to create
- `-o, --output` - The output path for the project
- `-s, --source` - The NuGet source to use for the project templates
- `-v, --version` - The version of the project templates to use

**Template Commands:**
- `aspire-starter` - Starter template
- `aspire` - AppHost and service defaults
- `aspire-apphost` - AppHost
- `aspire-servicedefaults` - Service defaults
- `aspire-test` - Integration tests

**Description:**
Creates a new Aspire project using the specified template. If no template is specified, prompts for template selection. Pulls the latest Aspire templates and creates the project using `dotnet new`.

### add

Add an integration to the Aspire project.

```cli
aspire add [<integration>] [options]
```

**Arguments:**
- `<integration>` - The name of the integration to add (e.g. redis, postgres)

**Options:**
- `--project` - The path to the project file to add the integration to
- `-v, --version` - The version of the integration to add
- `-s, --source` - The NuGet source to use for the integration

**Description:**
Adds an Aspire integration package to the project. If no integration name is provided, displays a selection prompt with available integrations. Integrations are given friendly names based on the package ID (e.g., `Aspire.Hosting.Redis` can be referenced as `redis`).

### publish

Generates deployment artifacts for an Aspire app host project. (Preview)

```cli
aspire publish [options] [[--] <additional arguments>...]
```

**Options:**
- `--project` - The path to the Aspire app host project file
- `-o, --output-path` - The output path for the generated artifacts

**Additional Arguments:**
Arguments passed to the application that is being run.

**Description:**
Generates deployment artifacts for the Aspire app host project using the default publisher.

### deploy

Deploy an Aspire app host project to its supported deployment targets. (Preview)

```cli
aspire deploy [options] [[--] <additional arguments>...]
```

**Options:**
- `--project` - The path to the Aspire app host project file
- `-o, --output-path` - The output path for deployment artifacts

**Additional Arguments:**
Arguments passed to the application that is being run.

**Description:**
Deploys an Aspire app host project to its supported deployment targets. Generates deployment artifacts and initiates the deployment process.

### exec

Run an Aspire app host to execute a command against the resource. (Preview)

```cli
aspire exec [options] [[--] <additional arguments>...]
```

**Options:**
- `--project` - The path to the Aspire app host project file
- `-r, --resource` - The name of the target resource to execute the command against
- `-s, --start-resource` - The name of the target resource to start and execute the command against

**Additional Arguments:**
Arguments passed to the application that is being run.

**Description:**
Runs the Aspire app host and executes a command against a specified resource. Use either `--resource` for an existing resource or `--start-resource` to start a resource and then execute the command.

### config

Manage configuration settings.

```cli
aspire config [command] [options]
```

**Subcommands:**

#### get
Get a configuration value.

```cli
aspire config get <key>
```

**Arguments:**
- `<key>` - The configuration key to get

#### set
Set a configuration value.

```cli
aspire config set <key> <value> [options]
```

**Arguments:**
- `<key>` - The configuration key to set
- `<value>` - The configuration value to set

**Options:**
- `-g, --global` - Set the configuration value globally in `$HOME/.aspire/settings.json` instead of the local settings file

#### list
List all configuration values.

```cli
aspire config list
```

#### delete
Delete a configuration value.

```cli
aspire config delete <key> [options]
```

**Arguments:**
- `<key>` - The configuration key to delete

**Options:**
- `-g, --global` - Delete the configuration value from the global settings file instead of the local settings file

**Description:**
Manages CLI configuration settings. Configuration can be set locally (per project) or globally (user-wide). Local settings are stored in the current directory, while global settings are stored in `$HOME/.aspire/settings.json`.