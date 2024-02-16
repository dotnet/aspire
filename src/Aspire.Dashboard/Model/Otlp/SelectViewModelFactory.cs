// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

public class SelectViewModelFactory
{
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
                    Id = new ResourceTypeDetails(OtlpApplicationType.Singleton, app.InstanceId),
                    Name = app.ApplicationName
                });

                continue;
            }

            // add a disabled "Resource" as a header
            selectViewModels.Add(new SelectViewModel<ResourceTypeDetails>
            {
                Id = new ResourceTypeDetails(OtlpApplicationType.ReplicaSet, null),
                Name = applicationName
            });

            // add each individual replica
            selectViewModels.AddRange(replicas.Select(replica =>
                new SelectViewModel<ResourceTypeDetails>
                {
                    Id = new ReplicaTypeDetails(OtlpApplicationType.Replica, replica.InstanceId, applicationName),
                    Name = ResourceFormatter.GetName(replica.ApplicationName, replica.InstanceId)
                }));
        }

        return selectViewModels;
    }
}
