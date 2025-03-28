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
/// Represents a context for publishing Azure Resource Manager (ARM) templates for a distributed application.
/// </summary>
/// <remarks>
/// This context facilitates the generation of ARM templates using the provided application model,
/// publisher options, and execution context. It handles resource configuration and ensures
/// that the ARM template is created in the specified output path.
/// </remarks>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
internal sealed class AzurePublishingContext(
    AzurePublisherOptions publisherOptions,
    AzureProvisioningOptions provisioningOptions,
    ILogger logger)
{
    private ILogger Logger => logger;
    private AzurePublisherOptions PublisherOptions => publisherOptions;
    private AzureProvisioningOptions ProvisioningOptions => provisioningOptions;

    public Infrastructure Infra = new()
    {
        TargetScope = DeploymentScope.Subscription
    };

    internal void WriteModel(DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(PublisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            Logger.LogInformation("No resources found in the model");
            return;
        }

        WriteAzureArtifactsOutput(model);
    }

    private void WriteAzureArtifactsOutput(DistributedApplicationModel model)
    {
        var outputDirectory = new DirectoryInfo(PublisherOptions.OutputPath!);
        outputDirectory.Create();

        var environmentParam = new ProvisioningParameter("environmentName", typeof(string));
        Infra.Add(environmentParam);

        var locationParam = new ProvisioningParameter("location", typeof(string));
        Infra.Add(locationParam);

        var principalId = new ProvisioningParameter("principalId", typeof(string));
        Infra.Add(principalId);

        var tags = new ProvisioningVariable("tags", typeof(object))
        {
            Value = new BicepDictionary<string>
            {
                ["aspire-env-name"] = environmentParam
            }
        };

        // REVIEW: Do we want people to be able to change this
        var rg = new ResourceGroup("rg")
        {
            Name = BicepFunction.Interpolate($"rg-{environmentParam}"),
            Location = locationParam,
            Tags = tags
        };

        // Process the resources in the model and create a module for each one
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

        // Resolve parameters *after* writing the modules to disk
        // this is because some parameters are added in ConfigureInfrastructure callbacks
        var parameterMap = new Dictionary<ParameterResource, ProvisioningParameter>();

        foreach (var resource in model.Resources.OfType<AzureBicepResource>())
        {
            foreach (var parameter in resource.Parameters)
            {
                Visit(parameter.Value, v =>
                {
                    if (v is ParameterResource p && !parameterMap.ContainsKey(p))
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

                        // Map the parameter to the Bicep parameter
                        parameterMap[p] = pp;
                    }
                });
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
                // resourceGroup(rgName)
                string rgName => new FunctionCallExpression(new IdentifierExpression("resourceGroup"), new StringLiteralExpression(rgName)),
                ParameterResource p => parameterMap[p],
                _ => new IdentifierExpression(rg.BicepIdentifier)
            };

            var module = moduleMap[resource];
            module.Scope = scope;
            module.Parameters.Add("location", locationParam);

            foreach (var parameter in resource.Parameters)
            {
                // TODO: There are a set of known parameter names that we may not be able to resolve.
                // This is from earlier versions of aspire where infra was split across
                // azd and aspire. Once the infra moves to aspire, we can throw for 
                // unresolved "known parameters".

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

        // Now find all resources that have deployment targets that are bicep modules
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var targetAnnotation) &&
                targetAnnotation.DeploymentTarget is AzureBicepResource br)
            {
                var moduleDirectory = outputDirectory.CreateSubdirectory(resource.Name);

                var modulePath = Path.Combine(moduleDirectory.FullName, $"{resource.Name}.bicep");

                var file = br.GetBicepTemplateFile();

                File.Copy(file.Path, modulePath, true);

                // TODO: Resolve parameters for the module and
                // handle flowing outputs from other modules

                foreach (var parameter in br.Parameters)
                {
                    Visit(parameter.Value, v =>
                    {
                        if (v is BicepOutputReference bo)
                        {
                            // Any bicep output reference needs to be propagated to the top level
                            outputs[bo.ValueExpression] = bo;
                        }
                    });
                }
            }
        }

        // Add parameters to the infrastructure
        foreach (var (_, pp) in parameterMap)
        {
            Infra.Add(pp);
        }

        // Add the parameters to the infrastructure
        Infra.Add(tags);

        // Add the resource group to the infrastructure
        Infra.Add(rg);

        // Add the modules to the infrastructure
        foreach (var (_, module) in moduleMap)
        {
            // Add the module to the infrastructure
            Infra.Add(module);
        }

        // Add the outputs to the infrastructure
        foreach (var (_, output) in outputs)
        {
            var module = moduleMap[output.Resource];

            var identifier = Infrastructure.NormalizeBicepIdentifier($"{output.Resource.Name}_{output.Name}");

            var bicepOutput = new ProvisioningOutput(identifier, typeof(string))
            {
                Value = GetOutputs(module, output.Name)
            };

            Infra.Add(bicepOutput);
        }

        SaveToDisk(outputDirectory.FullName, Infra);
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

    private void SaveToDisk(string outputDirectoryPath, Infrastructure infrastructure)
    {
        var plan = infrastructure.Build(ProvisioningOptions.ProvisioningBuildOptions);
        var compiledBicep = plan.Compile().First();

        logger.LogDebug("Writing Bicep module {BicepName}.bicep to {TargetPath}", infrastructure.BicepName, outputDirectoryPath);

        var bicepPath = Path.Combine(outputDirectoryPath, $"{infrastructure.BicepName}.bicep");
        File.WriteAllText(bicepPath, compiledBicep.Value);
    }
}