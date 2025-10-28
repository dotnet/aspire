---
applyTo: "tests/agent-scenarios/**/prompt.md"
---

# Test Scenario Prompt Instructions

This document provides comprehensive guidelines for authoring `prompt.md` files used with the `/test-scenario` workflow command in the Aspire repository.

## Purpose

Test scenario `prompt.md` files define automated exploratory testing scenarios that are executed by GitHub Copilot agents in the `dotnet/aspire-playground` repository. These scenarios validate that changes to Aspire work correctly in realistic development workflows, catching integration issues that unit tests might miss.

## How Test Scenarios Work

When a developer or reviewer comments `/test-scenario scenario-name` on a pull request:

1. The `test-scenario.yml` workflow validates the scenario name
2. Looks for `tests/agent-scenarios/scenario-name/prompt.md`
3. Reads the prompt content from the file
4. Creates an issue in the `aspire-playground` repository with:
   - The prompt content as instructions
   - Context from the source PR (PR number, URL, repository)
   - Assignment to the `copilot-swe-agent`
5. The agent executes the scenario in the playground repository
6. Results are tracked in the created issue and may include a PR with the test application

This provides end-to-end validation that the changes in the PR work correctly in a realistic development environment.

## Directory Structure

Test scenarios are located in `tests/agent-scenarios/` with the following structure:

```text
tests/agent-scenarios/
├── README.md                          # Documentation for all scenarios
├── scenario-name/
│   └── prompt.md                      # The scenario prompt
├── another-scenario/
│   └── prompt.md
└── ...
```

### Scenario Name Requirements

Scenario names must follow strict formatting rules to work with the workflow:

- **Must be lowercase**
- **Can contain alphanumeric characters (a-z, 0-9)**
- **Can use single hyphens (-) as word separators**
- **No consecutive hyphens**
- **No leading or trailing hyphens**

**Valid examples:**
- `redis-cache`
- `postgres-db`
- `azure-storage`
- `cli-new-command`

**Invalid examples:**
- `RedisCache` (uppercase)
- `redis_cache` (underscore)
- `redis--cache` (consecutive hyphens)
- `-redis-cache` (leading hyphen)
- `redis-cache-` (trailing hyphen)

## Prompt.md File Format

### Basic Structure

A `prompt.md` file should be written as clear, actionable instructions for an AI agent. The format can vary based on the complexity of the scenario:

**Simple format (for straightforward scenarios):**

```markdown
# Scenario Title

Brief description of what this scenario tests.

Detailed instructions for the agent, written in imperative mood:
1. Step one with specific commands
2. Step two with expected outcomes
3. Step three with verification steps
```

**Comprehensive format (for complex scenarios):**

```markdown
# Scenario Title

## Overview

Brief description of what this scenario validates and why it's important.

This smoke test validates that:
1. Key capability one
2. Key capability two
3. Key capability three

## Prerequisites

List any prerequisites or environment requirements:
- Docker installed and running (if needed)
- Python 3.11 or later (if needed)
- Network access to download packages

**Note**: The .NET SDK is not required as a prerequisite - the Aspire CLI will install it automatically.

## Step 1: First Major Task

### 1.1 Subtask

Detailed instructions with commands:

\```bash
# Command to execute
aspire --version
\```

**Expected outcome:**
- What should happen
- What to verify

### 1.2 Another Subtask

Continue with substeps...

## Step 2: Next Major Task

...continue with additional steps...

## Success Criteria

The scenario is considered **PASSED** if:
1. Criterion one
2. Criterion two
3. Criterion three

The scenario is considered **FAILED** if:
- Failure condition one
- Failure condition two

## Troubleshooting Tips

If issues occur during the scenario:

### Issue Type
- Diagnostic steps
- Possible solutions

## Notes for Agent Execution

Special instructions for the AI agent:
1. Capture screenshots at key points
2. Save detailed logs
3. Timing considerations
```

### Key Principles for Writing Prompts

1. **Be Explicit**: Don't assume the agent knows context. Explain what needs to be done and why.

2. **Use Imperative Commands**: Write instructions as direct commands rather than suggestions.
   - ✅ "Run `aspire new` to create a new application"
   - ❌ "You might want to create a new application"

3. **Include Expected Outcomes**: After each command or step, describe what should happen.

4. **Provide Verification Steps**: Include steps to verify that each action succeeded.

5. **Reference Existing Knowledge**: For complex operations, refer to existing documentation or patterns:
   ```markdown
   Follow the CLI acquisition instructions already provided in the aspire-playground 
   repository to obtain the native AOT build of the CLI for this PR.
   ```

6. **Include Success/Failure Criteria**: Clearly define what constitutes a passing or failing scenario.

7. **Use Code Blocks**: Format all commands, code, and file paths in appropriate code blocks with language identifiers.

8. **Capture Evidence**: Instruct the agent to take screenshots, save logs, or capture other evidence.

9. **Consider Prerequisites**: Explicitly state what tools or dependencies are needed.

10. **Think End-to-End**: Scenarios should test realistic workflows from start to finish.

## Example Scenarios

### Simple Scenario Example: Starter App

```markdown
# Starter App Scenario

Create an aspire application starting by downloading the Aspire CLI and creating a starter app.
```

This simple prompt works because:
- The aspire-playground repository has established patterns for CLI acquisition
- Creating a starter app is a well-known workflow
- The agent has context from existing documentation

### Complex Scenario Example: Smoke Test

See `tests/agent-scenarios/smoke-test-dotnet/prompt.md` for a comprehensive example that includes:
- Detailed step-by-step instructions
- Multiple verification points
- Screenshot capture requirements
- Success/failure criteria
- Troubleshooting guidance
- Notes for agent execution

This complex prompt is appropriate when:
- Testing multiple integrated features
- Validating critical workflows
- Requiring specific evidence capture
- Testing new or complex functionality

## When to Create Test Scenarios

### Agent Behavior: Never Create Automatically

**CRITICAL**: The coding agent should **NEVER** automatically create test scenarios or `prompt.md` files unless the developer explicitly requests it.

Creating scenarios requires understanding:
- What aspects of the feature need integration testing
- What realistic workflows should be validated
- What prerequisites and setup are required
- How to verify success meaningfully

Only developers can make these decisions. The agent may suggest creating a scenario, but should never create one without explicit approval.

### When to Suggest Creating Scenarios

The coding agent **SHOULD** add a comment to the PR suggesting the developer consider creating a test scenario in these situations:

#### 1. Adding Major New Hosting Integrations

When a PR adds a new hosting integration package (e.g., `Aspire.Hosting.NewTechnology`), especially:

- **Local-only integrations** that don't require cloud deployments:
  - Databases (Redis, PostgreSQL, MongoDB, SQL Server)
  - Message queues (RabbitMQ, Kafka)
  - Caches and search engines (Elasticsearch, Meilisearch)
  - Container-based services

- **Integrations with emulator support**:
  - Azure services with local emulators
  - AWS services with LocalStack support

**Why**: These can be easily tested in the playground environment without requiring cloud credentials or subscriptions.

**Example suggestion comment**:
```markdown
💡 **Test Scenario Suggestion**

This PR adds a new hosting integration for [Technology]. Consider creating a test 
scenario to validate the end-to-end developer experience:

- Create a scenario directory: `tests/agent-scenarios/[technology]-integration/`
- Add a `prompt.md` file that tests:
  - Installing the Aspire CLI from this PR build
  - Creating a new Aspire app
  - Adding the [Technology] resource to the AppHost
  - Running the application and verifying the resource works correctly
  - Checking the Dashboard shows the resource properly

Example: See `tests/agent-scenarios/smoke-test-dotnet/` for a comprehensive template.

To test the scenario, comment `/test-scenario [technology]-integration` on this PR.
```

#### 2. Adding Major New Client Integrations

When a PR adds a new client component package (e.g., `Aspire.NewTechnology.Client`), especially:

- **Integrations for local services**:
  - Database clients (Npgsql, MySqlConnector, StackExchange.Redis)
  - Messaging clients (RabbitMQ.Client, Confluent.Kafka)
  - Storage clients that work locally

- **Integrations with significant new APIs**:
  - New connection patterns
  - New configuration models
  - New health check or telemetry features

**Why**: Client integrations are the primary way developers interact with Aspire components. Testing them in realistic scenarios catches configuration issues, DI problems, and integration bugs.

**Example suggestion comment**:
```markdown
💡 **Test Scenario Suggestion**

This PR adds a new client integration for [Technology]. Consider creating a test 
scenario to validate the developer experience:

- Create a scenario directory: `tests/agent-scenarios/[technology]-client/`
- Add a `prompt.md` file that tests:
  - Creating an Aspire app with the [Technology] hosting and client packages
  - Configuring the client in a service project
  - Making actual calls to the [Technology] service
  - Verifying telemetry, health checks, and logging work correctly

Example: See `tests/agent-scenarios/smoke-test-dotnet/` for patterns on testing 
end-to-end connectivity.

To test the scenario, comment `/test-scenario [technology]-client` on this PR.
```

#### 3. Adding New Commands to Aspire CLI

When a PR adds a new command or significant functionality to the Aspire CLI (`src/Aspire.Cli/`):

- **New top-level commands**: `aspire newcommand`
- **New subcommands**: `aspire existing newsubcommand`
- **Significant changes to existing commands**: New options, changed behavior, new workflows

**Why**: CLI commands are the entry point for developers. Testing them in realistic scenarios ensures they work correctly with actual projects, handle errors gracefully, and provide good user experience.

**Example suggestion comment**:
```markdown
💡 **Test Scenario Suggestion**

This PR adds/modifies the `aspire [command]` CLI command. Consider creating a test 
scenario to validate the command works correctly:

- Create a scenario directory: `tests/agent-scenarios/cli-[command]/`
- Add a `prompt.md` file that tests:
  - Acquiring the Aspire CLI from this PR build
  - Running `aspire [command]` in various contexts
  - Verifying expected outputs and behaviors
  - Testing error handling and edge cases

Example: See `tests/agent-scenarios/smoke-test-dotnet/` for CLI testing patterns.

To test the scenario, comment `/test-scenario cli-[command]` on this PR.
```

#### 4. Other Scenarios to Consider

The agent should also suggest test scenarios for:

- **New project templates**: When adding or modifying Aspire project templates
  - Create scenarios that test creating and running projects from the template
  - Verify all template options work correctly

- **Dashboard features**: When adding significant new Dashboard capabilities
  - Create scenarios that exercise the new UI features
  - Include screenshot capture of the new functionality

- **Breaking changes to developer-facing APIs**: When making changes that affect how developers use Aspire
  - Create scenarios that validate the new API patterns work correctly
  - Test migration paths from old to new APIs

- **Service discovery changes**: When modifying how services discover and connect to each other
  - Create scenarios with multiple services communicating
  - Verify connections work across different patterns

- **Deployment/publishing changes**: When modifying how Aspire apps are deployed
  - Create scenarios that test the full deployment workflow
  - Verify generated artifacts are correct

### When NOT to Suggest Scenarios

The agent should **NOT** suggest test scenarios for:

- **Minor bug fixes**: Small corrections that don't change behavior significantly
- **Internal refactoring**: Changes to internal implementation that don't affect public APIs
- **Documentation updates**: Changes only to markdown files, comments, or docs
- **Test code changes**: Modifications only to test projects
- **Build/CI changes**: Changes to build scripts or workflows
- **Cloud-only services**: Integrations that require paid cloud subscriptions
  - Azure services without emulators
  - AWS services without LocalStack support
  - Third-party SaaS services requiring accounts

## Commenting Format for Suggestions

When suggesting a test scenario, use this format:

```markdown
💡 **Test Scenario Suggestion**

[One paragraph explaining what was added/changed and why a scenario would be valuable]

**Suggested scenario**: `tests/agent-scenarios/[scenario-name]/`

**What to test**:
- [Key testing point 1]
- [Key testing point 2]
- [Key testing point 3]

**Reference**: See `tests/agent-scenarios/[similar-scenario]/` for a template.

**To test**: Comment `/test-scenario [scenario-name]` on this PR.
```

Keep suggestions concise, actionable, and helpful. The goal is to remind developers, not to be prescriptive.

## Best Practices

### DO

✅ **Write prompts from the agent's perspective**: Assume the agent is starting fresh with only the repository context.

✅ **Break complex scenarios into steps**: Use numbered steps and clear section headers.

✅ **Include verification at each step**: Don't just run commands—verify they worked.

✅ **Specify exact commands**: Use code blocks with the exact commands to run.

✅ **Define success criteria**: Be explicit about what constitutes passing vs. failing.

✅ **Reference existing patterns**: Point to existing documentation or workflows when applicable.

✅ **Test realistic workflows**: Scenarios should mirror how real developers would use the feature.

✅ **Capture evidence**: Request screenshots, logs, or other artifacts that prove the scenario worked.

✅ **Consider the environment**: Remember that scenarios run in the aspire-playground repo, which has:
- Linux environment (Ubuntu)
- Docker available
- Common development tools (git, curl, etc.)
- Browser automation tools (playwright)

### DON'T

❌ **Don't be vague**: Avoid instructions like "test the feature" without specifics.

❌ **Don't assume context**: Don't assume the agent knows about PRs, issues, or features being tested.

❌ **Don't skip verification**: Every action should have a verification step.

❌ **Don't make scenarios too narrow**: Scenarios should test meaningful workflows, not single function calls.

❌ **Don't require manual intervention**: Scenarios must be fully automatable.

❌ **Don't test cloud-only services**: Avoid scenarios requiring paid subscriptions or cloud credentials.

❌ **Don't duplicate unit test coverage**: Scenarios are for integration testing, not unit testing.

❌ **Don't create scenarios without request**: Never automatically create scenarios—only suggest them.

## Testing Your Scenario

After creating a scenario, test it by:

1. **Commit the prompt.md file** to your PR branch
2. **Comment on the PR**: `/test-scenario your-scenario-name`
3. **Monitor the workflow**: Check the workflow run in the Actions tab
4. **Review the created issue**: Follow the link to see the agent's work in aspire-playground
5. **Iterate if needed**: Update the prompt based on results and test again

## Common Patterns

### Pattern: CLI Installation

Most scenarios need to install the Aspire CLI from the PR build:

```markdown
## Step 1: Install the Aspire CLI from the PR Build

The aspire-playground repository includes comprehensive instructions for acquiring 
different versions of the CLI, including PR builds.

**Follow the CLI acquisition instructions already provided in the aspire-playground 
repository to obtain the native AOT build of the CLI for this PR.**

Once acquired, verify the CLI is installed correctly:

\```bash
aspire --version
\```

Expected output should show the version matching the PR build.
```

### Pattern: Creating an Application

Standard pattern for creating a new Aspire application:

```markdown
## Step 2: Create a New Aspire Application

Use `aspire new` to create a new application:

\```bash
aspire new
\```

Follow the interactive prompts to select the desired template and options.

Verify the project structure:

\```bash
ls -la
\```

Expected structure:
- `AppName.sln` - Solution file
- `AppName.AppHost/` - The Aspire AppHost project
- Additional project directories based on template selection
```

### Pattern: Running and Verifying

Standard pattern for running and verifying an application:

```markdown
## Step 3: Run the Application

Start the application:

\```bash
aspire run
\```

Wait for startup (30-60 seconds) and note the Dashboard URL from the output.

### 3.1 Verify the Dashboard

Navigate to the Dashboard using the URL from the output:

\```bash
playwright-browser navigate $DASHBOARD_URL
playwright-browser take_screenshot --filename dashboard.png
\```

Expected: Dashboard loads successfully and screenshot shows all resources running.
```

### Pattern: Capturing Screenshots

For UI validation:

```markdown
## Step 4: Capture Visual Evidence

Take screenshots of key interfaces:

\```bash
# Dashboard overview
playwright-browser navigate http://localhost:XXXXX
playwright-browser take_screenshot --filename dashboard-main.png

# Web application
playwright-browser navigate http://localhost:YYYYY
playwright-browser take_screenshot --filename web-app.png
\```

Verify screenshots show:
- Dashboard with all resources in "Running" state
- Web application displaying correctly
```

## Version History

- **v1.0** (2025-10): Initial guidelines for test scenario prompts

## Related Documentation

- `tests/agent-scenarios/README.md` - Overview of all scenarios
- `.github/workflows/test-scenario.yml` - The workflow that executes scenarios
- Existing scenarios in `tests/agent-scenarios/*/prompt.md` - Examples to reference

## Questions or Issues

If you have questions about creating test scenarios or suggestions for improving these guidelines, please:

1. Open an issue in the dotnet/aspire repository
2. Tag it with the `area-testing` label
3. Reference these instructions in your issue
