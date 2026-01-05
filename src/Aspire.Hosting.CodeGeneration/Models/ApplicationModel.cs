// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models;

public sealed class CodeGenApplicationModel : IDisposable
{
    public required AssemblyLoaderContext AssemblyLoaderContext { get; init; }
    public required Dictionary<string, IntegrationModel> IntegrationModels { get; init; }
    public required Dictionary<RoType, ResourceModel> ResourceModels { get; init; }
    public required HashSet<RoType> ModelTypes { get; init; }
    public required string AppPath { get; init; }
    public required IWellKnownTypes WellKnownTypes { get; init; }
    public required DistributedApplicationBuilderModel BuilderModel { get; init; }

    private bool _disposedValue;

    public static CodeGenApplicationModel Create(IEnumerable<IntegrationModel> integrationModels, string appPath, AssemblyLoaderContext assemblyLoaderContext)
    {
        var knownTypes = integrationModels.Any() ? integrationModels.FirstOrDefault()?.WellKnownTypes! : new WellKnownTypes(assemblyLoaderContext);

        var integrationModelsLookup = integrationModels.ToDictionary(x => x.AssemblyName);

        // Get initial resources from integrations (concrete types implementing IResource)
        // Use DistinctBy to handle potential duplicates across integrations
        var initialResources = integrationModels.SelectMany(x => x.Resources)
            .DistinctBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        // Discover extension methods for each resource model across all integrations.
        // This may discover interface types used in IResourceBuilder<T> which get added to integration.Resources.
        foreach (var rm in initialResources.Values)
        {
            foreach (var integrationModel in integrationModels)
            {
                rm.DiscoverOpenGenericExtensionMethods(integrationModel);
            }
        }

        // Re-aggregate resources after discovery (includes interface types discovered above)
        // Use DistinctBy to handle interface types discovered in multiple integrations
        var resourceModels = integrationModels.SelectMany(x => x.Resources)
            .DistinctBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

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

        // Create the builder model from reflection
        var builderModel = DistributedApplicationBuilderModelFactory.Create(knownTypes);

        return new CodeGenApplicationModel
        {
            IntegrationModels = integrationModelsLookup,
            ResourceModels = resourceModels,
            ModelTypes = modelTypes,
            AppPath = appPath,
            WellKnownTypes = knownTypes,
            AssemblyLoaderContext = assemblyLoaderContext,
            BuilderModel = builderModel
        };
    }

    private void Dispose(bool disposing)
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
    ~CodeGenApplicationModel()
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
