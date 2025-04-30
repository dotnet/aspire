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
    AzurePublisherOptions publisherOptions,
    AzureProvisioningOptions provisioningOptions,
    ILogger logger)
{
    private ILogger Logger => logger;
    private AzurePublisherOptions PublisherOptions => publisherOptions;

    /// <summary>
    /// Gets the main.bicep infrastructure for the distributed application.
    /// </summary>
    public Infrastructure MainInfrastructure = new()
    {
        TargetScope = DeploymentScope.Subscription
    };

    /// <summary>
    /// Writes the specified distributed application model to the output path using Bicep templates.
    /// </summary>
    /// <param name="model">The distributed application model to write to the output path.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the async operation.</returns>
    public async Task WriteModelAsync(DistributedApplicationModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(PublisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            Logger.LogInformation("No resources found in the model");
            return;
        }

        await WriteAzureArtifactsOutputAsync(model, cancellationToken).ConfigureAwait(false);

        await SaveToDiskAsync(PublisherOptions.OutputPath).ConfigureAwait(false);
    }

    private Task WriteAzureArtifactsOutputAsync(DistributedApplicationModel model, CancellationToken _)
    {
        var outputDirectory = new DirectoryInfo(PublisherOptions.OutputPath!);
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
        }

        var environmentParam = new ProvisioningParameter("environmentName", typeof(string));
        MainInfrastructure.Add(environmentParam);

        var locationParam = new ProvisioningParameter("location", typeof(string));
        MainInfrastructure.Add(locationParam);

        var principalId = new ProvisioningParameter("principalId", typeof(string));
        MainInfrastructure.Add(principalId);

        var principalName = new ProvisioningParameter("principalName", typeof(string));
        MainInfrastructure.Add(principalName);

        var tags = new ProvisioningVariable("tags", typeof(object))
        {
            Value = new BicepDictionary<string>
            {
                ["aspire-env-name"] = environmentParam
            }
        };

        var rg = new ResourceGroup("rg")
        {
            Name = BicepFunction.Interpolate($"rg-{environmentParam}"),
            Location = locationParam,
            Tags = tags
        };

        var moduleMap = new Dictionary<AzureBicepResource, ModuleImport>();

        foreach (var resource in model.Resources.OfType<AzureBicepResource>())
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

        var parameterMap = new Dictionary<ParameterResource, ProvisioningParameter>();

        void MapParameter(object candidate)
        {
            if (candidate is ParameterResource p && !parameterMap.ContainsKey(p))
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

                parameterMap[p] = pp;
            }
        }

        foreach (var resource in model.Resources.OfType<AzureBicepResource>())
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
            ParameterResource p => parameterMap[p],
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

        foreach (var resource in model.Resources.OfType<AzureBicepResource>())
        {
            BicepValue<string> scope = resource.Scope?.ResourceGroup switch
            {
                string rgName => new FunctionCallExpression(new IdentifierExpression("resourceGroup"), new StringLiteralExpression(rgName)),
                ParameterResource p => new FunctionCallExpression(new IdentifierExpression("resourceGroup"), parameterMap[p].Value.Compile()),
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

                if (parameter.Key == AzureBicepResource.KnownParameters.UserPrincipalName && parameter.Value is null)
                {
                    module.Parameters.Add(parameter.Key, principalName);
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

        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation() is { } annotation && annotation.DeploymentTarget is AzureBicepResource br)
            {
                var moduleDirectory = outputDirectory.CreateSubdirectory(resource.Name);

                var modulePath = Path.Combine(moduleDirectory.FullName, $"{resource.Name}.bicep");

                var file = br.GetBicepTemplateFile();

                File.Copy(file.Path, modulePath, true);

                // Capture any bicep outputs from the registry info as it may be needed
                Visit(annotation.ContainerRegistryInfo?.Name, CaptureBicepOutputs);
                Visit(annotation.ContainerRegistryInfo?.Endpoint, CaptureBicepOutputs);

                if (annotation.ContainerRegistryInfo is IAzureContainerRegistry acr)
                {
                    Visit(acr.ManagedIdentityId, CaptureBicepOutputs);
                }

                foreach (var parameter in br.Parameters)
                {
                    Visit(parameter.Value, CaptureBicepOutputs);
                }
            }
        }

        foreach (var (_, pp) in parameterMap)
        {
            MainInfrastructure.Add(pp);
        }

        MainInfrastructure.Add(tags);
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

            MainInfrastructure.Add(bicepOutput);
        }

        return Task.CompletedTask;
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
