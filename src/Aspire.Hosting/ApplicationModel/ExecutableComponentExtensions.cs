// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public static class ExecutableComponentExtensions
{
    public static IEnumerable<ExecutableComponent> GetExecutableComponents(this DistributedApplicationModel model)
    {
        return model.Components.OfType<ExecutableComponent>();
    }
}
