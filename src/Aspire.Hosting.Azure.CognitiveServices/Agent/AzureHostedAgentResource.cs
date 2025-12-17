// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

    /// <summary>
    /// CPU allocation for each hosted agent instance.
    /// </summary>
    public decimal Cpu
    {
        get;
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
            field = value;
        }
    } = 1;

    /// <summary>
    /// Memory allocation for each hosted agent instance, in GiB
    /// </summary>
    public decimal Memory
    {
        get;
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
            field = value;
        }
    } = 2;

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
    public async Task<AgentVersionCreationOptions> ToAgentVersionCreationOptionsAsync(DistributedApplicationExecutionContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (!Target.TryGetContainerImageName(out var imageName))
        {
            throw new InvalidOperationException($"Resource '{Target.Name}' does not have a container image name.");
        }
        var def = new HostedAgentConfiguration(
            new ImageBasedHostedAgentDefinition(
                [new ProtocolVersionRecord(AgentCommunicationMethod.Responses, "v1"), new ProtocolVersionRecord(AgentCommunicationMethod.ActivityProtocol, "v2")],
                "1", "2Gi",
                image: await ((IValueProvider)Image).GetValueAsync(cancellationToken).ConfigureAwait(false)
            )
        );
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
                    def.Definition.EnvironmentVariables[key] = stringValue;
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

        var options = new AgentVersionCreationOptions(def.Definition)
        {
            Description = def.Description,
        };
        foreach (var (key, value) in def.Metadata)
        {
            options.Metadata[key] = value;
        }
        return options;
    }

    /// <summary>
    /// Publishes the hosted agent during the manifest publishing phase.
    /// </summary>
    public async Task PublishAsync(ManifestPublishingContext ctx)
    {
        var opts = await ToAgentVersionCreationOptionsAsync(ctx.ExecutionContext, NullLogger.Instance, ctx.CancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Writing agent manifest for path {ctx.ManifestPath}");
        ctx.Writer.WriteString("type", "azurefoundry.hostedagent.v0");
        ctx.Writer.WriteString("description", Description);
        ctx.Writer.WriteString("image", Image.ValueExpression);
        ctx.Writer.WriteStartObject("definition");
        ctx.Writer.WriteString("cpu", opts.Definition.Cpu);
        ctx.Writer.WriteString("memory", opts.Definition.Memory);
        ctx.Writer.WriteEndObject();
        ctx.Writer.WriteStartObject("metadata");
        foreach (var property in Metadata)
        {
            var key = await property.Key.GetValueAsync().ConfigureAwait(false);
            var value = await property.Value.GetValueAsync().ConfigureAwait(false);
            if (key is null || value is null)
            {
                continue;
            }
            ctx.Writer.WritePropertyName(key);
            ctx.Writer.WriteString(key, value);
        }
        ctx.Writer.WriteEndObject();
        ctx.Writer.WriteEndObject();
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
        var options = await ToAgentVersionCreationOptionsAsync(context).ConfigureAwait(false);
        var projectClient = new AIProjectClient(new Uri(projectEndpoint), new DefaultAzureCredential());
        var result = await projectClient.Agents.CreateAgentVersionAsync(
            Name,
            options,
            context.CancellationToken
        ).ConfigureAwait(false);
        return result.Value;
    }
}
