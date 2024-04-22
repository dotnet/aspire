// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

public static class SelectViewModelHelpers
{
    public static SelectViewModel<ResourceTypeDetails> GetApplication(this List<SelectViewModel<ResourceTypeDetails>> applications, string? name, SelectViewModel<ResourceTypeDetails> fallback)
    {
        if (name is null)
        {
            return fallback;
        }

        return applications.SingleOrDefault(e => e.Id?.Type is OtlpApplicationType.ReplicaInstance or OtlpApplicationType.Singleton && string.Equals(name, e.Name, StringComparisons.ResourceName)) ?? fallback;
    }

    public static List<SelectViewModel<ResourceTypeDetails>> CreateApplicationsSelectViewModel(List<OtlpApplication> applications)
    {
        var replicasByApplicationName = OtlpApplication.GetReplicasByApplicationName(applications);

        var selectViewModels = new List<SelectViewModel<ResourceTypeDetails>>();

        foreach (var (applicationName, replicas) in replicasByApplicationName)
        {
            if (replicas.Count == 1)
            {
                // not replicated
                var app = replicas.Single();
                selectViewModels.Add(new SelectViewModel<ResourceTypeDetails>
                {
                    Id = ResourceTypeDetails.CreateSingleton(app.InstanceId),
                    Name = app.ApplicationName
                });

                continue;
            }

            // add a disabled "Resource" as a header
            selectViewModels.Add(new SelectViewModel<ResourceTypeDetails>
            {
                Id = ResourceTypeDetails.CreateReplicaSet(applicationName),
                Name = applicationName
            });

            // add each individual replica
            selectViewModels.AddRange(replicas.Select(replica =>
                new SelectViewModel<ResourceTypeDetails>
                {
                    Id = ResourceTypeDetails.CreateReplicaInstance(replica.InstanceId, applicationName),
                    Name = replica.InstanceId
                }));
        }

        return selectViewModels;
    }
}
