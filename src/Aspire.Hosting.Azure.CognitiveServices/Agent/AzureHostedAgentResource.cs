// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Azure.CognitiveServices;

/// <summary>
/// An Azure AI Foundry hosted agent resource.
/// </summary>
public class AzureHostedAgentResource : Resource, IComputeResource, IResourceWithEnvironment
{
    /// <summary>
    /// Creates a new instance of the <see cref="AzureHostedAgentResource"/> class.
    /// </summary>
    public AzureHostedAgentResource([ResourceName] string name, IResource target, Action<HostedAgentConfiguration>? configure = null) : base(name)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
        Configure = configure;
        Annotations.Add(new ManifestPublishingCallbackAnnotation(PublishAsync));
        // Set up steps for deploying this particular hosted agent
        Annotations.Add(new PipelineStepAnnotation(async (ctx) =>
        {
            List<PipelineStep> steps = [];
            PipelineStep? pushStep = null;
            var deploymentAnnotation = Target.GetDeploymentTargetAnnotation() ?? throw new InvalidOperationException($"Deployment target annotation is required on resource '{Target.Name}' to deploy as hosted agent.");
            var project = deploymentAnnotation.ComputeEnvironment as AzureCognitiveServicesProjectResource
                ?? throw new InvalidOperationException($"Compute environment for resource '{Target.Name}' must be an AzureCognitiveServicesProjectResource to deploy as hosted agent.");
            if (Target.RequiresImageBuildAndPush())
            {
                pushStep = new PipelineStep
                {
                    Name = $"push-{Target.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await AzureEnvironmentResourceHelpers.PushImageToRegistryAsync(
                            project,
                            Target,
                            ctx,
                            containerImageBuilder).ConfigureAwait(false);
                        ctx.ReportingStep.Log(LogLevel.Information, $"Successfully pushed image for **{Target.Name}** to registry **{project.Name}**", enableMarkdown: true);
                    },
                    Resource = Target,
                    Tags = [WellKnownPipelineTags.PushContainerImage],
                    RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                    DependsOnSteps = [AzureEnvironmentResource.ProvisionInfrastructureStepName]
                };
                steps.Add(pushStep);
            }
            // Create a step to deploy container as agent
            var agentDeployStep = new PipelineStep
            {
                Name = $"deploy-{Name}",
                Action = async (ctx) =>
                {
                    var version = await DeployAsync(ctx, project).ConfigureAwait(false);
                    ctx.ReportingStep.Log(LogLevel.Information, $"Successfully deployed **{Name}** as Hosted Agent (version {version})", enableMarkdown: true);
                    // TODO: set the value on the agent resource
                },
                Tags = [WellKnownPipelineTags.DeployCompute],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                Resource = this,
                DependsOnSteps = [WellKnownPipelineSteps.DeployPrereq, AzureEnvironmentResource.ProvisionInfrastructureStepName]
            };
            steps.Add(agentDeployStep);
            if (pushStep is not null)
            {
                agentDeployStep.DependsOn(pushStep);
            }
            return steps;
        }));

        // Wire up pipeline steps we introduced above
        Annotations.Add(new PipelineConfigurationAnnotation(async (context) =>
        {
            // BuildCompute = build Docker images, so do that before pushing
            context.GetSteps(Target, WellKnownPipelineTags.BuildCompute).RequiredBy(context.GetSteps(Target, WellKnownPipelineTags.PushContainerImage));
        }));
    }

    /// <summary>
    /// Configuration action to customize the hosted agent definition during deployment.
    /// </summary>
    public Action<HostedAgentConfiguration>? Configure { get; set; }

    private decimal _cpu = 1.0m;

    /// <summary>
    /// CPU allocation for each hosted agent instance.
    /// </summary>
    public decimal Cpu
    {
        get => _cpu;
        set
        {
            if (value < 0.25m || value > 3.5m)
            {
                throw new ArgumentOutOfRangeException(nameof(Cpu), "CPU must be between 0.25 and 3.5 cores.");
            }
            if (value % 0.25m != 0)
            {
                throw new ArgumentException("CPU must be in increments of 0.25 cores.", nameof(Cpu));
            }
            _cpu = value;
        }
    }

    /// <summary>
    /// Memory allocation for each hosted agent instance, in GiB.
    /// Must be 2x the CPU allocation.
    /// </summary>
    public decimal Memory
    {
        get => _cpu * 2;
        set
        {
            if (value < 0.5m || value > 7m)
            {
                throw new ArgumentOutOfRangeException(nameof(Memory), "Memory must be between 0.5 and 7 GiB.");
            }
            if (value % 0.5m != 0)
            {
                throw new ArgumentException("Memory must be in increments of 0.5 GiB.", nameof(Memory));
            }
            _cpu = value / 2;
        }
    }

    /// <summary>
    /// The description of the hosted agent.
    /// </summary>
    public string Description { get; set; } = "Azure Hosted Agent Resource";
    /// <summary>
    /// Additional metadata to associate with the hosted agent.
    /// </summary>
    public IDictionary<IValueProvider, IValueProvider> Metadata { get; set; } = new Dictionary<IValueProvider, IValueProvider>();

    /// <summary>
    /// Once deployed, the version that is assigned to this hosted agent.
    /// </summary>
    public ReferenceExpression Version { get; } = ReferenceExpression.Create($"latest");

    /// <summary>
    /// The fully qualified image name for the hosted agent.
    /// </summary>
    public ContainerImageReference Image => new(Target);

    /// <summary>
    /// The target containerized workload that this hosted agent deploys.
    /// </summary>
    public IResource Target { get; }

    /// <summary>
    /// Convert all dynamic values into concrete values for deployment.
    /// </summary>
    public async Task<HostedAgentConfiguration> ToHostedAgentConfigurationAsync(DistributedApplicationExecutionContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (!Target.TryGetContainerImageName(out var imageName))
        {
            throw new InvalidOperationException($"Resource '{Target.Name}' does not have a container image name.");
        }
        var def = new HostedAgentConfiguration(imageName);
        if (Target.TryGetEnvironmentVariables(out var envVars))
        {
            void callback(string key, object? rawVal, string? stringValue, Exception? exc)
            {
                if (exc is not null)
                {
                    throw new InvalidOperationException($"Error resolving environment variable '{key}' for hosted agent '{Name}': {exc.Message}", exc);
                }
                if (stringValue is not null)
                {
                    def.EnvironmentVariables[key] = stringValue;
                }
                else
                {
                    logger.LogWarning("Environment variable '{Key}' for hosted agent '{Name}' resolved to null and will be skipped.", key, Name);
                }
            }
            await Target.ProcessEnvironmentVariableValuesAsync(context, callback, logger, cancellationToken).ConfigureAwait(false);
        }
        def.Description = Description;
        foreach (var (key, value) in Metadata)
        {
            var keyResolved = await key.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var valueResolved = await value.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (keyResolved is null || valueResolved is null)
            {
                continue;
            }
            def.Metadata[keyResolved] = valueResolved;
        }
        if (Configure is not null)
        {
            Configure(def);
        }
        return def;
    }

    /// <summary>
    /// Publishes the hosted agent during the manifest publishing phase.
    /// </summary>
    public async Task PublishAsync(ManifestPublishingContext ctx)
    {
        var def = await ToHostedAgentConfigurationAsync(ctx.ExecutionContext, NullLogger.Instance, ctx.CancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Writing agent manifest for path {ctx.ManifestPath}");
        ctx.Writer.WriteString("type", "azurefoundry.hostedagent.v0");
        ctx.Writer.WriteStartObject("definition");
        ctx.Writer.WriteString("description", def.Description);
        ctx.Writer.WriteString("image", def.Image);
        ctx.Writer.WriteString("cpu", def.CpuString);
        ctx.Writer.WriteString("memory", def.MemoryString);
        ctx.Writer.WriteStartObject("environmentVariables");
        foreach (var envVar in def.EnvironmentVariables)
        {
            ctx.Writer.WritePropertyName(envVar.Key);
            ctx.Writer.WriteString(envVar.Key, envVar.Value);
        }
        ctx.Writer.WriteEndObject(); // environmentVariables
        ctx.Writer.WriteStartObject("metadata");
        foreach (var property in def.Metadata)
        {
            ctx.Writer.WritePropertyName(property.Key);
            ctx.Writer.WriteString(property.Key, property.Value);
        }
        ctx.Writer.WriteEndObject(); // metadata
        ctx.Writer.WriteEndObject(); // definition
        ctx.TryAddDependentResources(Target);
    }

    /// <summary>
    /// Deploys the specified agent to the given Azure Cognitive Services project.
    /// </summary>
    public async Task<AgentVersion> DeployAsync(PipelineStepContext context, AzureCognitiveServicesProjectResource project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var projectEndpoint = await project.ConnectionString.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(projectEndpoint))
        {
            throw new InvalidOperationException($"Project '{project.Name}' does not have a valid connection string.");
        }
        var def = await ToHostedAgentConfigurationAsync(context.ExecutionContext, context.Logger, context.CancellationToken).ConfigureAwait(false);
        var projectClient = new AIProjectClient(new Uri(projectEndpoint), new DefaultAzureCredential());
        var result = await projectClient.Agents.CreateAgentVersionAsync(
            Name,
            def.ToAgentVersionCreationOptions(),
            context.CancellationToken
        ).ConfigureAwait(false);
        return result.Value;
    }
}
