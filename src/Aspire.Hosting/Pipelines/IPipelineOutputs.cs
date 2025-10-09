// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

internal interface IPipelineOutputs
{
    void Set<T>(string key, T value);
    bool TryGet<T>(string key, [NotNullWhen(true)] out T? value);
}
