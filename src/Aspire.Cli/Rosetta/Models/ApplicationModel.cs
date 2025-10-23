// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

internal class ApplicationModel : IDisposable
{
    public required AssemblyLoaderContext AssemblyLoaderContext { get; init; }
    public required Dictionary<string, IntegrationModel> IntegrationModels { get; init; }
    public required Dictionary<RoType, ResourceModel> ResourceModels { get; init; }
    public required HashSet<RoType> ModelTypes { get; init; }
    public required string AppPath { get; init; }
    public required IWellKnownTypes WellKnownTypes { get; init; }

    // Custom method names
    public List<Mapping> MethodMappings = [];
    private bool _disposedValue;

    public static ApplicationModel Create(IEnumerable<IntegrationModel> integrationModels, string appPath, AssemblyLoaderContext assemblyLoaderContext)
    {
        var knownTypes = integrationModels.Any() ? integrationModels.FirstOrDefault()?.WellKnownTypes! : new WellKnownTypes(assemblyLoaderContext);

        var integrationModelsLookup = integrationModels.ToDictionary(x => x.AssemblyName);
        var resourceModels = integrationModels.SelectMany(x => x.Resources).ToDictionary(x => x.Key, x => x.Value);

        // Discover extension methods for each resource model across all integrations. This needs to be done after all integrations are loaded.
        foreach (var rm in resourceModels.Values)
        {
            foreach (var integrationModel in integrationModels)
            {
                rm.DiscoverOpenGenericExtensionMethods(integrationModel);
            }
        }

        // Discover all model types across all integrations
        var modelTypes = new HashSet<RoType>();

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
            AssemblyLoaderContext = assemblyLoaderContext
        };
    }

    public bool TryGetMapping(string methodName, RoType[] parameterTypes, [NotNullWhen(true)] out Mapping? mapping)
    {
        mapping = MethodMappings.FirstOrDefault(x => x.MethodName == methodName && x.ParameterTypes.SequenceEqual(parameterTypes));
        return mapping != null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            AssemblyLoaderContext.Dispose();
            _disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ApplicationModel()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
