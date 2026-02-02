# Plan for Hosted Agent DevEx

The desired outcome here is that the developer needs to do very little in order to get up and running with:

- A locally running agent that can use Azure Foundry models and tools, with local visualization of observability.
- An automatic (Github Actions or Azure DevOps Pipeline) deployment as a versioned Hosted Agent in Foundry.
- The ability to define multiple remote environments (e.g. "staging" and "production") with minimal change.
- The ability to export their infra definitions (except for the agent) to `.bicep` files that an infra/ops team can deploy, for organizations where devs cannot deploy their own infra in production.

The prerequisites for the dev would be:

- `az` installed and logged in
- An Azure subscription + resource group where the user is an CognitiveServices contributor (for remote dev resources)
- `python` installed (with `pip` or `uv` as a package manager)
- `dotnet` and `aspire` installed
- `docker` installed, if the dev wants to deploy

Then the developer experience would be:

```shell
# Generate a project via interactive question/answer for
# various settings (e.g. which package manager, which
# agent SDK).
aspire new aspire-hostedagent-starter

# Run various local dev tasks
aspire do fmt
aspire do test
aspire do lint

# For running locally with remote models and tools
aspire run

# For deploying to Production
aspire deploy

# For deploying to an alternate "Staging" environment
aspire deploy -e Staging
```

This will also provision AppInsights observability (including traces), where the output during local dev will be to the Aspire local web dashboard, and in production will be to Azure tools.

## The project structure

The template will generate a project that looks something like this:

```
- README.md  - Explanatory text for how to run/use the project
- AGENTS.md   - A starter file for coding agents to use this project
- .github/
  - actions/  - Github Actions workflows for CI and deployment
- app/  - The Python agent project
  - pyproject.toml
  - src/  - Actual Python code
  - test/  - `pytest` code
- apphost.cs  - The Aspire app "definitions"
- appsettings.Development.json  - Settings to use for local dev
- appsettings.Staging.json  - Settings to use for deploying to production
- appsettings.Production.json  - Settings to use for deploying to production
```

Most of the developer's time will be spent in the `app` directory, ideally. They would be able to drop their existing code mostly into the `app` directory and run it, with a few tweaks to wrap it with the Hosted Agent framework. In addition, affordances will be made to make coding agents more effective in this project.

## Advanced Mode

There will also be a template for a more "enterprise" version of this Agent with BYO CosmosDB, Storage, and VNet, as well as more locked down managed identities using identity blueprints. The dev would instantiate that like this:

```shell
aspire new aspire-enterpriseagent-starter
```

In this scenario, in anticipation of Staging/Production having more locked down permissions, devs would be able to export Bicep templates that an ops/infra team can run, since managing RBAC and Identities (even for agents) often requires fairly high level permissions.

## Coding Agent Affordances

A few things will be including in the templates to make coding agents more effective in these projects:

- An AGENTS.md file that describes the overall shape of the project.
- Skills for using less common Aspire commands like configuration, cache management, local secrets, etc.
- Run modes for the Agent so that logs and telemetry are printed to stdout or log files so that agents can access them (as opposed to the Aspire dashboard, which is less accessible).
