# Custom Agents for Aspire Repository

This directory contains custom agent definitions that extend GitHub Copilot's capabilities for the dotnet/aspire repository. These agents are specialized tools designed to help with specific development tasks.

## Available Agents

### issue-reproducer

**File**: `issue-reproducer.agent.md`  
**Purpose**: Investigates GitHub issues and creates reproduction tests or sample projects to help developers understand and fix bugs.

**What it does**:
- Fetches and analyzes GitHub issues
- Extracts problem descriptions, reproduction steps, and environment details
- Creates xUnit test cases that reproduce reported bugs
- Creates standalone sample projects when tests are insufficient
- Documents missing information when issues lack sufficient detail
- Generates comprehensive PR descriptions

**When to use**:
- You have a GitHub issue describing a bug
- You need a reproducible test case for a reported problem
- You want to create a sample project demonstrating an issue
- You need to investigate and understand a bug report

**Example invocations**:
```
Create a reproduction test for https://github.com/dotnet/aspire/issues/12345
Investigate issue #12345 and create a repro that demonstrates the problem
Create a sample project that reproduces the bug in issue #12345
```

**Key features**:
- Does NOT fix issues - only reproduces them
- Creates tests that fail initially and pass once the bug is fixed
- Follows repository testing conventions (xUnit SDK v3)
- Handles incomplete issue descriptions gracefully
- Uses Skip or QuarantinedTest attributes appropriately

---

### connectionproperties

**File**: `connectionproperties.agent.md`  
**Purpose**: Specialized agent for creating and improving Connection Properties in Aspire resource and README files.

**What it does**:
- Implements `IResourceWithConnectionString.GetConnectionProperties` for Aspire resources
- Documents connection properties in README files
- Ensures proper inheritance for child resources
- Creates JDBC connection strings where applicable

**When to use**:
- Adding or modifying Aspire hosting resources
- Implementing connection string functionality
- Documenting resource connection properties

**Tools**: read, search, edit

---

### test-disabler

**File**: `test-disabler.md`  
**Purpose**: Quarantines or disables flaky/problematic tests using the QuarantineTools utility.

**What it does**:
- Uses the `tools/QuarantineTools` project to quarantine or disable tests
- Adds `[QuarantinedTest]` or `[ActiveIssue]` attributes to test methods
- Handles multiple tests efficiently
- Verifies tests are properly skipped after quarantine
- Creates comprehensive PR descriptions

**When to use**:
- Tests are flaky and failing intermittently
- Tests need to be disabled due to known issues
- Tests need to be re-enabled after fixes

**Example invocations**:
```
Disable HealthChecksRegistersHealthCheckService with https://github.com/dotnet/aspire/issues/11820
Quarantine these tests: TestMethod1, TestMethod2 with issue #12345
Re-enable TestMethodName that was quarantined
```

**Tools**: bash, view, edit

---

## Agent File Format

Custom agents are defined using markdown files with YAML frontmatter:

```markdown
---
name: agent-name
description: Brief description of what this agent does
tools: ["bash", "view", "edit", "search", "read", "create"]
---

Detailed instructions for the agent in markdown format...
```

### Frontmatter Fields

- **name**: Unique identifier for the agent (kebab-case)
- **description**: One-line description of the agent's purpose
- **tools**: Array of tools the agent can use (bash, view, edit, search, read, create, github-mcp-server)

### Instruction Format

The markdown body contains:
- Mission statement and purpose
- Understanding user requests
- Step-by-step task execution instructions
- Code examples and templates
- Error handling guidelines
- Response format specifications
- Important constraints and guidelines

## Creating a New Agent

1. Create a new `.agent.md` file in this directory
2. Add YAML frontmatter with name, description, and tools
3. Write comprehensive instructions following the pattern of existing agents
4. Test the agent by invoking it in a GitHub issue or PR
5. Document the agent in this README

## Reference

For more examples of custom agents, see:
- [GitHub Awesome Copilot Agents](https://github.com/github/awesome-copilot/tree/main/agents)
- [C# Expert Agent](https://github.com/github/awesome-copilot/blob/main/agents/CSharpExpert.agent.md)

## Notes

- Agents are invoked by GitHub Copilot when appropriate for the task
- Agents have their own context window and receive specific prompts
- Agents can use the tools specified in their frontmatter
- Well-written agents should be self-contained and comprehensive
