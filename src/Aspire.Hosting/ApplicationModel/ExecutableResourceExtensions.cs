// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public static class ExecutableResourceExtensions
{
    public static IEnumerable<ExecutableResource> GetExecutableResources(this DistributedApplicationModel model)
    {
        return model.Resources.OfType<ExecutableResource>();
    }
}
