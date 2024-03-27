// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

internal static class DistributedApplicationModelExtensions
{
    public static bool TryGetAppResource(this DistributedApplicationModel model, [NotNullWhen(returnValue: true)]out IAppResource? appResource)
    {
        appResource = model.Resources.OfType<IAppResource>().SingleOrDefault();
        return appResource is not null;
    }
}
