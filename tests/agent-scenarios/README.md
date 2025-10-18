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
4. Create an agent task using the GitHub CLI
5. Post a comment with the scenario name and link to the agent's PR

## Creating a New Scenario

1. Create a new directory under `tests/agent-scenarios/` with a valid scenario name
2. Add a `prompt.md` file with the prompt text for the agent
3. Commit the changes
4. Test by commenting `/test-scenario your-scenario-name` on a PR

## Example Scenarios

### starter-app

Creates a basic Aspire starter application.

**Prompt:** Create an aspire application starting by downloading the Aspire CLI and creating a starter app.
