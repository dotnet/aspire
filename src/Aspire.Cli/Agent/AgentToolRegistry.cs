// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire.Cli.Agent.Tools;
using Microsoft.Extensions.AI;

namespace Aspire.Cli.Agent;

/// <summary>
/// Registry that provides all available agent tools.
/// </summary>
internal sealed class AgentToolRegistry : IAgentToolRegistry
{
    private readonly IAspireNewTool _newTool;
    private readonly IAspireAddTool _addTool;
    private readonly IAspireRunTool _runTool;
    private readonly IAspireDoctorTool _doctorTool;
    private readonly IListIntegrationsTool _listIntegrationsTool;
    private readonly IGetIntegrationDocsTool _getIntegrationDocsTool;

    public AgentToolRegistry(
        IAspireNewTool newTool,
        IAspireAddTool addTool,
        IAspireRunTool runTool,
        IAspireDoctorTool doctorTool,
        IListIntegrationsTool listIntegrationsTool,
        IGetIntegrationDocsTool getIntegrationDocsTool)
    {
        _newTool = newTool;
        _addTool = addTool;
        _runTool = runTool;
        _doctorTool = doctorTool;
        _listIntegrationsTool = listIntegrationsTool;
        _getIntegrationDocsTool = getIntegrationDocsTool;
    }

    public IList<AIFunction> GetTools(AgentContext context)
    {
        var tools = new List<AIFunction>();

        // Always available tools
        tools.Add(AIFunctionFactory.Create(
            async ([Description("The template to use (e.g., 'aspire-starter', 'aspire')")] string template,
                   [Description("The name for the new project")] string name,
                   [Description("The output directory (optional)")] string? outputDir) =>
                await _newTool.ExecuteAsync(template, name, outputDir),
            "aspire_new",
            "Create a new Aspire project from a template"));

        tools.Add(AIFunctionFactory.Create(
            async () => await _doctorTool.ExecuteAsync(),
            "aspire_doctor",
            "Run diagnostics to check the Aspire environment (Docker, .NET SDK, certificates, etc.)"));

        tools.Add(AIFunctionFactory.Create(
            async ([Description("Optional search filter")] string? filter) =>
                await _listIntegrationsTool.ExecuteAsync(filter),
            "list_integrations",
            "List available Aspire integrations with their hosting and client package pairs"));

        tools.Add(AIFunctionFactory.Create(
            async ([Description("The integration package ID (e.g., 'Aspire.Hosting.Redis')")] string packageId) =>
                await _getIntegrationDocsTool.ExecuteAsync(packageId),
            "get_integration_docs",
            "Get detailed documentation for a specific Aspire integration including usage examples and configuration"));

        // Tools that require an AppHost
        if (!context.IsOfflineMode)
        {
            tools.Add(AIFunctionFactory.Create(
                async ([Description("The integration to add (e.g., 'redis', 'postgres', 'rabbitmq')")] string integration) =>
                    await _addTool.ExecuteAsync(integration, context.AppHostProject!.FullName),
                "aspire_add",
                "Add an integration to the AppHost project"));

            tools.Add(AIFunctionFactory.Create(
                async ([Description("Whether to watch for changes")] bool watch) =>
                    await _runTool.ExecuteAsync(context.AppHostProject!.FullName, watch),
                "aspire_run",
                "Run the Aspire AppHost application"));
        }

        return tools;
    }
}
