// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Dashboard;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Proto.V1;

partial class Resource
{
    public static Resource FromSnapshot(ResourceSnapshot snapshot)
    {
        Resource resource = new()
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            DisplayName = snapshot.DisplayName,
            Uid = snapshot.Uid,
            State = snapshot.State ?? "",
            StateStyle = snapshot.StateStyle ?? "",
        };

        if (snapshot.CreationTimeStamp.HasValue)
        {
            resource.CreatedAt = Timestamp.FromDateTime(snapshot.CreationTimeStamp.Value.ToUniversalTime());
        }

        foreach (var env in snapshot.Environment)
        {
            resource.Environment.Add(new EnvironmentVariable { Name = env.Name, Value = env.Value ?? "", IsFromSpec = env.IsFromSpec });
        }

        foreach (var url in snapshot.Urls)
        {
            resource.Urls.Add(new Url { Name = url.Name, FullUrl = url.Url, IsInternal = url.IsInternal });
        }

        foreach (var property in snapshot.Properties)
        {
            resource.Properties.Add(new ResourceProperty { Name = property.Name, Value = property.Value });
        }

        // Disable start/stop/restart commands until host/DCP infrastructure is ready.
        /*
        if (snapshot.ResourceType is KnownResourceTypes.Project or KnownResourceTypes.Container or KnownResourceTypes.Executable)
        {
            if (snapshot.State is "Exited" or "Finished" or "FailedToStart")
            {
                resource.Commands.Add(new ResourceCommand
                {
                    CommandType = "Start",
                    ConfirmationMessage = "ConfirmationMessage!",
                    DisplayName = "Start",
                    DisplayDescription = "Start resource",
                    IsHighlighted = true,
                    IconName = "Play"
                });
            }
            else
            {
                resource.Commands.Add(new ResourceCommand
                {
                    CommandType = "Stop",
                    ConfirmationMessage = "ConfirmationMessage!",
                    DisplayName = "Stop",
                    DisplayDescription = "Stop resource",
                    IsHighlighted = true,
                    IconName = "Stop"
                });
            }

            resource.Commands.Add(new ResourceCommand
            {
                CommandType = "Restart",
                ConfirmationMessage = "ConfirmationMessage!",
                DisplayName = "Restart",
                DisplayDescription = "Restart resource",
                IsHighlighted = false,
                IconName = "ArrowCounterclockwise"
            });
        }
        */

        return resource;
    }
}
