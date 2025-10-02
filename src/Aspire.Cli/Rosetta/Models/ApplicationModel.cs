// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Cli.Rosetta.Models;

public class ApplicationModel
{
    public required Dictionary<string, IntegrationModel> IntegrationModels { get; init; }
    public required Dictionary<Type, ResourceModel> ResourceModels { get; init; }
    public required HashSet<Type> ModelTypes { get; init; }
    public required string AppPath { get; init; }

    public required IWellKnownTypes WellKnownTypes { get; init; }

    // Custom method names
    public List<Mapping> MethodMappings = [];

    public static ApplicationModel Create(IEnumerable<IntegrationModel> integrationModels, string appPath)
    {
        var knownTypes = integrationModels.Any() ? integrationModels.FirstOrDefault()?.WellKnownTypes! : new WellKnownTypes([]);

        var integrationModelsLookup = integrationModels.ToDictionary(x => x.AssemblyName);
        var resourceModels = integrationModels.SelectMany(x => x.Resources).ToDictionary(x => x.Key, x => x.Value);

        // Discover extension methods for each resource model across all integrations
        foreach (var rm in resourceModels.Values)
        {
            foreach (var integrationModel in integrationModels)
            {
                rm.DiscoverExtensionMethods(integrationModel);
            }
        }

        // Discover all model types across all integrations
        var modelTypes = new HashSet<Type>();

        foreach (var integrationModel in integrationModels)
        {
            modelTypes.UnionWith(integrationModel.ModelTypes);

            foreach (var resourceModel in integrationModel.Resources.Values)
            {
                modelTypes.UnionWith(resourceModel.ModelTypes);
            }
        }

        var stringType = knownTypes.GetKnownType(typeof(string));

        return new ApplicationModel
        {
            IntegrationModels = integrationModelsLookup,
            ResourceModels = resourceModels,
            ModelTypes = modelTypes,
            AppPath = appPath,
            MethodMappings = [
                new("WithEnvironment", [stringType, stringType], "WithEnvironmentString"),
            ],
            WellKnownTypes = knownTypes,
        };
    }

    public bool TryGetMapping(string methodName, Type[] parameterTypes, [NotNullWhen(true)] out Mapping? mapping)
    {
        mapping = MethodMappings.FirstOrDefault(x => x.MethodName == methodName && x.ParameterTypes.SequenceEqual(parameterTypes));
        return mapping != null;
    }
}
