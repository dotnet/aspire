# Agent Scenarios

This directory contains scenario definitions for the `/test-scenario` workflow command.

## Structure

Each scenario is a subdirectory with a `prompt.md` file:

```text
agent-scenarios/
├── scenario-name/
│   └── prompt.md
└── another-scenario/
    └── prompt.md
```

## Scenario Name Requirements

- Must be lowercase
- Can contain alphanumeric characters (a-z, 0-9)
- Can use single hyphens (-) as word separators
- No consecutive hyphens
- No leading or trailing hyphens

**Valid examples:**
- `starter-app`
- `redis-cache`
- `postgres-db`
- `azure-deployment`

**Invalid examples:**
- `StarterApp` (uppercase)
- `starter_app` (underscore)
- `starter--app` (consecutive hyphens)
- `-starter-app` (leading hyphen)
- `starter-app-` (trailing hyphen)

## Usage

To trigger an agent scenario on a pull request, comment:

```bash
/test-scenario scenario-name
```

For example:

```bash
/test-scenario starter-app
```

The workflow will:
1. Validate the scenario name format
2. Look for `tests/agent-scenarios/scenario-name/prompt.md`
3. Read the prompt from the file
4. Create an issue in the `aspire-playground` repository with the prompt and PR context
5. Assign the issue to the GitHub Copilot agent
6. Wait for the agent to create a PR (up to 5 minutes)
7. Post a comment with links to both the issue and the agent's PR (if available)

## Creating a New Scenario

1. Create a new directory under `tests/agent-scenarios/` with a valid scenario name
2. Add a `prompt.md` file with the prompt text for the agent
3. Commit the changes
4. Test by commenting `/test-scenario your-scenario-name` on a PR

## Example Scenarios

### starter-app

Creates a basic Aspire starter application.

**Prompt:** Create an aspire application starting by downloading the Aspire CLI and creating a starter app.

### smoke-test-dotnet

Performs a comprehensive smoke test of an Aspire PR build by installing the Aspire CLI, creating a .NET Blazor-based starter application, and verifying its functionality including the Dashboard, API service, and frontend.

**Key features:**
- Tests the native AOT build of the Aspire CLI
- Creates and runs an Aspire starter app with Blazor frontend
- Verifies Dashboard functionality and telemetry collection
- Tests SDK install feature flag (`dotNetSdkInstallationEnabled`)
- Captures screenshots for verification

### smoke-test-python

Performs a comprehensive smoke test of an Aspire PR build by installing the Aspire CLI, creating a Python starter application with Vite/React frontend, and verifying its functionality.

**Key features:**
- Tests the native AOT build of the Aspire CLI
- Creates and runs an Aspire Python starter app (`aspire-py-starter`)
- Tests Python backend API service and Vite/React frontend
- Verifies Dashboard functionality and telemetry collection
- Tests SDK install feature flag (`dotNetSdkInstallationEnabled`)
- Tests hot reload for both Python and Vite
- Captures screenshots for verification

### deployment-docker

Tests the end-to-end workflow of creating an Aspire application, adding Docker Compose integration, and deploying it using Docker Compose.

**Key features:**
- Creates a new Aspire starter application
- Adds Docker Compose integration using `aspire add` command
- Updates AppHost to configure Docker Compose environment
- Generates Docker Compose files using `aspire publish`
- Deploys the application with `docker compose up`
- Verifies all service endpoints are accessible
- Tests service-to-service communication
- Cleans up deployment with `docker compose down`

### eshop-update

Tests the Aspire CLI's update functionality on the dotnet/eshop repository, validating that PR builds can successfully update real-world Aspire applications.

**Key features:**
- Downloads and integrates the dotnet/eshop repository
- Tests `aspire update` command on a complex, multi-service application
- Validates package version updates from PR builds
- Launches the updated application with `aspire run`
- Identifies and fixes simple package dependency issues
- Enumerates all packages requiring manual intervention
- Verifies Dashboard functionality and service health for multiple services
- Tests update logic on production-like application architecture
