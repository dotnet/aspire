# AGENT_NAME AI Foundry Hosted Agent-

This is a starting point for building an AI chat application using Azure AI Foundry. It has a local Python agent that can be run with `aspire run`, whereupon Aspire will provision and connect to resources in Azure. When `aspire deploy` is run, this will deploy as a Hosted Agent to an Azure subscription and provision resources as needed.

## How to develop

### Prerequisites

You must have the following in order to run this project locally

- dotnet CLI
- aspire CLI
- python
- uv
- az CLI (logged into the subscription you desire)

### Workflows

```bash
# To provision Azure resource dependencies like models and run agent locally
aspire run

# To containerize agent and deploy as a hosted agent
aspire deploy

# To test
aspire exec test

# Runs linters, typecheckers, and other static validators
aspire exec check
```
