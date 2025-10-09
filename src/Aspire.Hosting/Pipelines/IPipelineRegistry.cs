// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Pipelines;

internal interface IPipelineRegistry
{
    IEnumerable<PipelineStep> GetAllSteps();
    PipelineStep? GetStep(string name);
}
