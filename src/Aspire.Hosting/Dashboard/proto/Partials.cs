// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    IsHighlighted = true,
                    // Play - regular - 20px
                    IconContent = "<path d=\"M17.22 8.69a1.5 1.5 0 0 1 0 2.62l-10 5.5A1.5 1.5 0 0 1 5 15.5v-11A1.5 1.5 0 0 1 7.22 3.2l10 5.5Zm-.48 1.75a.5.5 0 0 0 0-.88l-10-5.5A.5.5 0 0 0 6 4.5v11c0 .38.4.62.74.44l10-5.5Z\"/>"
                });
            }
            else
            {
                resource.Commands.Add(new ResourceCommand
                {
                    CommandType = "Stop",
                    ConfirmationMessage = "ConfirmationMessage!",
                    DisplayName = "Stop",
                    IsHighlighted = true,
                    // Stop - regular - 20px
                    IconContent = "<path d=\"M15.5 4c.28 0 .5.22.5.5v11a.5.5 0 0 1-.5.5h-11a.5.5 0 0 1-.5-.5v-11c0-.28.22-.5.5-.5h11Zm-11-1C3.67 3 3 3.67 3 4.5v11c0 .83.67 1.5 1.5 1.5h11c.83 0 1.5-.67 1.5-1.5v-11c0-.83-.67-1.5-1.5-1.5h-11Z\"/>"
                });
            }

            resource.Commands.Add(new ResourceCommand
            {
                CommandType = "Restart",
                ConfirmationMessage = "ConfirmationMessage!",
                DisplayName = "Restart",
                IsHighlighted = false,
                // ArrowCounterclockwise - regular - 20px
                IconContent = "<path d=\"M16 10A6 6 0 0 0 5.53 6H7.5a.5.5 0 0 1 0 1h-3a.5.5 0 0 1-.5-.5v-3a.5.5 0 0 1 1 0v1.6a7 7 0 1 1-1.98 4.36.5.5 0 0 1 1 .08L4 10a6 6 0 0 0 12 0Z\"/>"
            });
        }
        */

        return resource;
    }
}
