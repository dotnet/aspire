# Aspire AI Foundry ChatKit Template

This is a starting point for building an enterprise-grade AI chat application using Azure AI Foundry and the OpenAI ChatKit framework.
This template provides a robust foundation for creating conversational AI solutions that can be customized to meet specific business needs.

## Resources provisioned

This template provisions the following resources in your Azure subscription:

In local development mode:

- AI Foundry account
    - GPT-5.1 deployment
    - Project
        - Tool Connection to example MCP server
- CosmosDB account
    - Container for ChatKit metadata storage
- Azure storage account
    - Azure Blob Service container for ChatKit attachment storage
- User-assigned managed identity for ChatKit API server
- Local instance of agent with ChatKit API adapter
- Local instance of ChatKit web server

In publish (a.k.a. pre-production and production) mode, the resources are similar but include networking and security enhancements:

- AI Foundry account
    - Managed Network (VNet)
        - Private Link project
    - GPT-5.1 deployment
        - Private endpoint
    - Project
        - Capability Host
            - Reference to CosmosDB account
            - Reference to Storage account
            - Reference to Managed Network VNet
        - Tool Connection to example MCP server
        - Hosted Agent with ChatKit API adapter
- APIM for ChatKit API server
- Azure Container Apps Environment
    - App with ChatKit web server
    - OAuth registration with Entra
- CosmosDB account
    - Container for Conversations/Responses API/Files storage
    - Private endpoint
- Azure storage account
    - Azure Blob Service container for ChatKit attachment storage
    - Private endpoint
- User-assigned managed identity for ChatKit API server
- Log Analytics workspace
    - Private endpoint
- Application Insights instance
    - Private endpoint
