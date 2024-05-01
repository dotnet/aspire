// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;

internal sealed class ProjectLambdaRuntimeEnvironment : ILambdaRuntimeEnvironment
{
    private readonly ProjectResource _project;

    public ProjectLambdaRuntimeEnvironment(ProjectResource project)
    {
        _project = project;
    }
}
