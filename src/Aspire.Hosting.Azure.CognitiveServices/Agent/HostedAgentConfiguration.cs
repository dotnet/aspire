// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Projects.OpenAI;

namespace Aspire.Hosting.Azure.CognitiveServices;

/// <summary>
/// A configuration helper for Python hosted agents.
///
/// This is used instead of AzureAgentVersionCreationOptions to provide better static
/// typing of the agent definition.
/// </summary>
public class HostedAgentConfiguration(ImageBasedHostedAgentDefinition definition)
{
    /// <summary>
    /// The description of the hosted agent.
    /// </summary>
    public string Description { get; set; } = "Python Hosted Agent";

    /// <summary>
    /// Additional metadata to associate with the hosted agent.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>()
    {
        { "DeployedBy", "Aspire Hosting Framework" },
        { "DeployedOn", DateTime.UtcNow.ToString("o") }
    };

    /// <summary>
    /// Agent definition
    /// </summary>
    public ImageBasedHostedAgentDefinition Definition { get; set; } = definition;
}
