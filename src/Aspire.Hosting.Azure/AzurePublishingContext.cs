// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a context for publishing Azure bicep templates for a distributed application.
/// </summary>
/// <remarks>
/// This context facilitates the generation of bicep templates using the provided application model,
/// publisher options, and execution context. It handles resource configuration and ensures
/// that the bicep template is created in the specified output path.
/// </remarks>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzurePublishingContext(
    string outputPath,
    AzureProvisioningOptions provisioningOptions,
    ILogger logger)
{
    private ILogger Logger => logger;

    /// <summary>
    /// Gets the main.bicep infrastructure for the distributed application.
    /// </summary>
    public Infrastructure MainInfrastructure = new()
    {
        TargetScope = DeploymentScope.Subscription
    };

    /// <summary>
    /// Gets a dictionary that maps parameter resources to provisioning parameters.
    /// </summary>
    /// <remarks>
    /// The value is the <see cref="ProvisioningParameter"/> of the <see cref="MainInfrastructure"/>
    /// that was created to be filled with the value of the Aspire <see cref="ParameterResource"/>.
    /// </remarks>
    public Dictionary<ParameterResource, ProvisioningParameter> ParameterLookup { get; } = [];

    /// <summary>
    /// Gets a dictionary that maps output references to provisioning outputs.
    /// </summary>
    /// <remarks>
    /// The value is the <see cref="ProvisioningOutput"/> of the <see cref="MainInfrastructure"/>
    /// that was created with the value of the output referenced by the Aspire <see cref="BicepOutputReference"/>.
    /// </remarks>
    public Dictionary<BicepOutputReference, ProvisioningOutput> OutputLookup { get; } = [];

    /// <summary>
    /// Writes the specified distributed application model to the output path using Bicep templates.
    /// </summary>
    /// <param name="model">The distributed application model to write to the output path.</param>
    /// <param name="environment">The Azure environment resource.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the async operation.</returns>
    public async Task WriteModelAsync(DistributedApplicationModel model, AzureEnvironmentResource environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(outputPath);

        if (model.Resources.Count == 0)
        {
            Logger.LogInformation("No resources found in the model");
            return;
        }

        await WriteAzureArtifactsOutputAsync(model, environment, cancellationToken).ConfigureAwait(false);

        await SaveToDiskAsync(outputPath).ConfigureAwait(false);
    }

    private Task WriteAzureArtifactsOutputAsync(DistributedApplicationModel model, AzureEnvironmentResource environment, CancellationToken _)
    {
        var outputDirectory = new DirectoryInfo(outputPath);
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }

        var bicepResourcesToPublish = model.Resources.OfType<AzureBicepResource>().ToList();

        MapParameter(environment.ResourceGroupName);
        MapParameter(environment.Location);
        MapParameter(environment.PrincipalId);

        var resourceGroupParam = ParameterLookup[environment.ResourceGroupName];
        MainInfrastructure.Add(resourceGroupParam);

        var locationParam = ParameterLookup[environment.Location];
        MainInfrastructure.Add(locationParam);

        var principalId = ParameterLookup[environment.PrincipalId];
        MainInfrastructure.Add(principalId);

        var rg = new ResourceGroup("rg")
        {
            Name = resourceGroupParam,
            Location = locationParam,
        };

        var moduleMap = new Dictionary<AzureBicepResource, ModuleImport>();

        foreach (var resource in bicepResourcesToPublish)
        {
            var file = resource.GetBicepTemplateFile();

            var moduleDirectory = outputDirectory.CreateSubdirectory(resource.Name);

            var modulePath = Path.Combine(moduleDirectory.FullName, $"{resource.Name}.bicep");

            File.Copy(file.Path, modulePath, true);

            var identifier = Infrastructure.NormalizeBicepIdentifier(resource.Name);

            var module = new ModuleImport(identifier, $"{resource.Name}/{resource.Name}.bicep")
            {
                Name = resource.Name
            };

            moduleMap[resource] = module;
        }

        foreach (var resource in bicepResourcesToPublish)
        {
            // Map parameters from existing resources
            if (resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAnnotation))
            {
                Visit(existingAnnotation.ResourceGroup, MapParameter);
                Visit(existingAnnotation.Name, MapParameter);
            }

            // Map parameters for the resource itself
            foreach (var parameter in resource.Parameters)
            {
                Visit(parameter.Value, MapParameter);
            }
        }

        static BicepValue<string> GetOutputs(ModuleImport module, string outputName) =>
            new MemberExpression(new MemberExpression(new IdentifierExpression(module.BicepIdentifier), "outputs"), outputName);

        BicepFormatString EvalExpr(ReferenceExpression expr)
        {
            var args = new object[expr.ValueProviders.Count];

            for (var i = 0; i < expr.ValueProviders.Count; i++)
            {
                args[i] = Eval(expr.ValueProviders[i]);
            }

            return new BicepFormatString(expr.Format, args);
        }

        object Eval(object? value) => value switch
        {
            BicepOutputReference b => GetOutputs(moduleMap[b.Resource], b.Name),
            ParameterResource p => ParameterLookup[p],
            ConnectionStringReference r => Eval(r.Resource.ConnectionStringExpression),
            IResourceWithConnectionString cs => Eval(cs.ConnectionStringExpression),
            ReferenceExpression re => EvalExpr(re),
            string s => s,
            _ => ""
        };

        static BicepValue<string> ResolveValue(object val)
        {
            return val switch
            {
                BicepValue<string> s => s,
                string s => s,
                ProvisioningParameter p => p,
                BicepFormatString fs => BicepFunction2.Interpolate(fs),
                _ => throw new NotSupportedException("Unsupported value type " + val.GetType())
            };
        }

        foreach (var resource in bicepResourcesToPublish)
        {
            BicepValue<string> scope = resource.Scope?.ResourceGroup switch
            {
                string rgName => new FunctionCallExpression(new IdentifierExpression("resourceGroup"), new StringLiteralExpression(rgName)),
                ParameterResource p => new FunctionCallExpression(new IdentifierExpression("resourceGroup"), ParameterLookup[p].Value.Compile()),
                _ => new IdentifierExpression(rg.BicepIdentifier)
            };

            var module = moduleMap[resource];
            module.Scope = scope;
            module.Parameters.Add("location", locationParam);

            foreach (var parameter in resource.Parameters)
            {
                if (parameter.Key == AzureBicepResource.KnownParameters.UserPrincipalId && parameter.Value is null)
                {
                    module.Parameters.Add(parameter.Key, principalId);
                    continue;
                }

                var value = ResolveValue(Eval(parameter.Value));

                module.Parameters.Add(parameter.Key, value);
            }
        }

        var outputs = new Dictionary<string, BicepOutputReference>();

        void CaptureBicepOutputs(object value)
        {
            if (value is BicepOutputReference bo)
            {
                outputs[bo.ValueExpression] = bo;
            }
        }

        void CaptureBicepOutputsFromParameters(IResourceWithParameters resource)
        {
            foreach (var parameter in resource.Parameters)
            {
                Visit(parameter.Value, CaptureBicepOutputs);
            }
        }

        // Capture any bicep outputs referenced from resources outside of the MainInfrastructure.
        // These include DeploymentTarget resources and any other resources that have parameters that reference bicep outputs.
        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation() is { } annotation && annotation.DeploymentTarget is AzureBicepResource br)
            {
                var moduleDirectory = outputDirectory.CreateSubdirectory(resource.Name);

                var modulePath = Path.Combine(moduleDirectory.FullName, $"{resource.Name}.bicep");

                var file = br.GetBicepTemplateFile();

                File.Copy(file.Path, modulePath, true);

                // Capture any bicep outputs from the registry info as it may be needed
                Visit(annotation.ContainerRegistry?.Name, CaptureBicepOutputs);
                Visit(annotation.ContainerRegistry?.Endpoint, CaptureBicepOutputs);

                if (annotation.ContainerRegistry is IAzureContainerRegistry acr)
                {
                    Visit(acr.ManagedIdentityId, CaptureBicepOutputs);
                }

                CaptureBicepOutputsFromParameters(br);
            }
            else if (resource is IResourceWithParameters rwp && !bicepResourcesToPublish.Contains(resource))
            {
                CaptureBicepOutputsFromParameters(rwp);
            }
        }

        foreach (var (_, pp) in ParameterLookup)
        {
            MainInfrastructure.Add(pp);
        }

        MainInfrastructure.Add(rg);

        foreach (var (_, module) in moduleMap)
        {
            MainInfrastructure.Add(module);
        }

        foreach (var (_, output) in outputs)
        {
            var module = moduleMap[output.Resource];

            var identifier = Infrastructure.NormalizeBicepIdentifier($"{output.Resource.Name}_{output.Name}");

            var bicepOutput = new ProvisioningOutput(identifier, typeof(string))
            {
                Value = GetOutputs(module, output.Name)
            };

            OutputLookup[output] = bicepOutput;
            MainInfrastructure.Add(bicepOutput);
        }

        return Task.CompletedTask;
    }

    private void MapParameter(object? candidate)
    {
        if (candidate is ParameterResource p && !ParameterLookup.ContainsKey(p))
        {
            var pid = Infrastructure.NormalizeBicepIdentifier(p.Name);

            var pp = new ProvisioningParameter(pid, typeof(string))
            {
                IsSecure = p.Secret
            };

            if (!p.Secret && p.Default is not null)
            {
                pp.Value = p.Value;
            }

            ParameterLookup[p] = pp;
        }
    }

    private static void Visit(object? value, Action<object> visitor) =>
        Visit(value, visitor, []);

    private static void Visit(object? value, Action<object> visitor, HashSet<object> visited)
    {
        if (value is null || !visited.Add(value))
        {
            return;
        }

        visitor(value);

        if (value is IValueWithReferences vwr)
        {
            foreach (var reference in vwr.References)
            {
                Visit(reference, visitor, visited);
            }
        }
    }

    /// <summary>
    /// Saves the compiled Bicep template to disk.
    /// </summary>
    /// <param name="outputDirectoryPath">The path to the output directory where the Bicep template will be saved.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    private async Task SaveToDiskAsync(string outputDirectoryPath)
    {
        var plan = MainInfrastructure.Build(provisioningOptions.ProvisioningBuildOptions);
        var compiledBicep = plan.Compile().First();

        logger.LogDebug("Writing Bicep module {BicepName}.bicep to {TargetPath}", MainInfrastructure.BicepName, outputDirectoryPath);

        var bicepPath = Path.Combine(outputDirectoryPath, $"{MainInfrastructure.BicepName}.bicep");
        await File.WriteAllTextAsync(bicepPath, compiledBicep.Value).ConfigureAwait(false);
    }
}
