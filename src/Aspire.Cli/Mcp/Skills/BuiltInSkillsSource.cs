// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Provides built-in skills with hardcoded content.
/// These skills contain MCP-specific knowledge (available tools, CLI commands)
/// that isn't available in external documentation.
/// </summary>
internal sealed class BuiltInSkillsSource
{
    private static readonly Dictionary<string, (string Description, string Content)> s_skills = new(StringComparer.OrdinalIgnoreCase)
    {
        [KnownSkills.AspirePairProgrammer] = (
            Description: "Activates an Aspire pair programmer persona with deep knowledge of Aspire concepts, best practices, and documentation.",
            Content: """
                # Aspire Pair Programmer Skill

                You are an expert Aspire pair programmer with deep knowledge of Aspire, distributed applications, and cloud-native development.

                ## Your Expertise

                - **Aspire architecture**: AppHost, ServiceDefaults, orchestration, and the resource model
                - **Integrations**: Redis, PostgreSQL, SQL Server, MongoDB, RabbitMQ, Kafka, Azure services, and 40+ other integrations
                - **Deployment**: Azure Container Apps, Kubernetes, Docker Compose, and custom deployment pipelines
                - **Observability**: OpenTelemetry, structured logging, distributed tracing, and the Aspire Dashboard
                - **Best practices**: Service discovery, health checks, configuration, and security

                ## Available Tools

                You have access to Aspire MCP tools. Use them to:

                ### Documentation Tools
                - **list_docs**: Browse all available aspire.dev documentation pages with titles and summaries
                - **search_docs**: Search documentation using keywords (e.g., 'redis connection string', 'health checks')
                - **get_doc**: Retrieve full content of a specific documentation page by slug

                ### Resource & Observability Tools
                - **list_resources**: Check the status of running resources
                - **list_console_logs**: View console output from resources
                - **list_structured_logs**: Analyze structured log entries
                - **list_traces**: Investigate distributed traces
                - **execute_resource_command**: Execute commands on resources (start, stop, restart)

                ### Integration & Environment Tools
                - **list_integrations**: Discover available Aspire integrations
                - **doctor**: Diagnose Aspire environment issues

                ## Guidelines

                1. **Use the Aspire CLI** for operations like running, publishing, and deploying apps. Not the .NET CLI directly.
                2. **Search documentation** with `search_docs` when answering questions about Aspire features or APIs.
                3. **Get full docs** with `get_doc` when you need detailed information on a specific topic.
                4. **Check resource status** before troubleshooting issues.
                5. **Provide code examples** that follow Aspire conventions and patterns.
                6. **Suggest integrations** from the available gallery when appropriate.
                """
        ),

        [KnownSkills.TroubleshootApp] = (
            Description: "Comprehensive troubleshooting for your Aspire application, analyzing resources, logs, traces, and providing recommendations.",
            Content: """
                # Troubleshoot App Skill

                This skill provides a systematic approach to troubleshooting Aspire applications.

                ## Troubleshooting Workflow

                ### Step 1: Environment Check
                - Use `doctor` to verify the Aspire environment is properly configured

                ### Step 2: Resource Status
                - Use `list_resources` to check the status and health of all resources
                - Identify any resources that are not running, unhealthy, or have errors

                ### Step 3: Log Analysis
                - For any problematic resources, use `list_console_logs` to check for startup errors
                - Use `list_structured_logs` to find error and warning level entries

                ### Step 4: Trace Analysis
                - Use `list_traces` to identify failed or slow operations
                - Look for patterns in the traces that might indicate the root cause

                ### Step 5: Documentation Lookup
                - Use `search_docs` to find relevant troubleshooting documentation for the symptom
                - Use `get_doc` to retrieve detailed documentation for specific topics

                ### Step 6: Recommendations
                Based on the analysis, provide:
                1. **Root cause** - What's likely causing the issue
                2. **Immediate fix** - Commands or changes to resolve the issue now
                3. **Long-term solution** - Best practices to prevent this issue in the future
                4. **Related resources** - Links or references to relevant documentation
                """
        ),

        [KnownSkills.DebugResource] = (
            Description: "Helps debug issues with a specific resource in your Aspire application.",
            Content: """
                # Debug Resource Skill

                This skill provides step-by-step guidance for debugging a specific resource.

                ## Debugging Workflow

                ### Step 1: Check Resource Status
                - Use `list_resources` to see the current state and health of the resource
                - Look for state (Running, Failed, Stopped) and health status

                ### Step 2: Review Console Logs
                - Use `list_console_logs` with the resource name to check for errors or warnings
                - Pay attention to startup messages and exception stack traces

                ### Step 3: Analyze Structured Logs
                - Use `list_structured_logs` filtered to the resource for detailed log entries
                - Filter by severity (Error, Warning) to find issues

                ### Step 4: Check Traces
                - Use `list_traces` to see if there are any failed requests or slow operations
                - Look for HTTP 5xx errors, timeouts, or exception traces

                ### Step 5: Search Documentation
                - Use `search_docs` with the resource type to find relevant documentation
                - Look for configuration requirements, connection strings, and common issues

                ### Step 6: Suggest Fixes
                Based on the findings:
                - Recommend configuration changes
                - Suggest resource commands (start, stop, restart)
                - Provide code fixes if applicable
                """
        ),

        [KnownSkills.AddIntegration] = (
            Description: "Guides you through adding a new integration (database, cache, messaging, etc.) to your Aspire application.",
            Content: """
                # Add Integration Skill

                This skill helps you add new integrations to your Aspire application.

                ## Integration Workflow

                ### Step 1: Find the Right Package
                - Use `list_integrations` to search for the appropriate Aspire hosting and client packages
                - Look for both `Aspire.Hosting.*` (AppHost) and `Aspire.*` (client) packages

                ### Step 2: Search Documentation
                - Use `search_docs` with the integration type to find relevant setup documentation
                - Look for configuration examples and best practices

                ### Step 3: Get Detailed Documentation
                - Use `get_doc` to retrieve the full documentation page for the integration
                - Pay attention to prerequisites and configuration options

                ### Step 4: Provide Configuration

                **AppHost Configuration** (Program.cs):
                ```csharp
                var builder = DistributedApplication.CreateBuilder(args);

                // Add the resource
                var resource = builder.AddXxx("name")
                    .WithPersistence();

                // Reference from services
                builder.AddProject<Projects.MyService>("myservice")
                    .WithReference(resource);
                ```

                **Client Configuration** (Program.cs in consuming project):
                ```csharp
                builder.AddXxxClient("name");
                ```

                ### Step 5: Best Practices
                - Use connection strings from configuration
                - Enable health checks
                - Consider persistence for local development
                - Use Aspire CLI commands (`aspire add`) rather than `dotnet add`
                """
        ),

        [KnownSkills.DeployApp] = (
            Description: "Guides you through deploying your Aspire application to a target environment (Azure, Kubernetes, Docker Compose, etc.).",
            Content: """
                # Deploy App Skill

                This skill guides you through deploying Aspire applications.

                ## Deployment Workflow

                ### Step 1: Check Environment
                - Use `doctor` to verify your Aspire environment is properly configured
                - Ensure all prerequisites for the target platform are installed

                ### Step 2: Search Deployment Docs
                - Use `search_docs` with the deployment target to find documentation
                - Look for platform-specific requirements and configuration

                ### Step 3: Get Detailed Documentation
                - Use `get_doc` to retrieve the full deployment guide
                - Pay attention to prerequisites and authentication requirements

                ### Step 4: Publishing

                Generate deployment artifacts:
                ```bash
                aspire publish --output-path ./publish --publisher <target>
                ```

                Common publishers:
                - `azure` - Azure Container Apps
                - `kubernetes` - Kubernetes manifests
                - `docker-compose` - Docker Compose files

                ### Step 5: Deployment

                Deploy to the target:
                ```bash
                aspire deploy --environment <environment>
                ```

                ### Step 6: Configuration
                - Set environment-specific configuration
                - Configure secrets using the target platform's secret management
                - Set up monitoring and alerting

                ### Step 7: Verification
                - Check resource health in the target environment
                - Verify endpoints are accessible
                - Test key functionality

                ## Best Practices
                - Use Aspire CLI commands (`aspire publish`, `aspire deploy`) rather than direct cloud CLI commands
                - Set up CI/CD pipelines for automated deployments
                - Use separate environments for staging and production
                - Configure health checks and readiness probes
                """
        )
    };

    /// <summary>
    /// Lists all built-in skills.
    /// </summary>
    public static ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future use

        var skills = s_skills.Select(kvp => new SkillInfo
        {
            Name = kvp.Key,
            Description = kvp.Value.Description
        }).ToList();

        return new ValueTask<IReadOnlyList<SkillInfo>>(skills);
    }

    /// <summary>
    /// Gets a built-in skill by name.
    /// </summary>
    public static ValueTask<SkillContent?> GetSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future use

        if (s_skills.TryGetValue(skillName, out var skill))
        {
            var content = new SkillContent
            {
                Name = skillName,
                Content = skill.Content.Trim()
            };
            return new ValueTask<SkillContent?>(content);
        }

        return new ValueTask<SkillContent?>((SkillContent?)null);
    }
}
