// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta;

internal interface IDependencyContext
{
    public string ArtifactsPath { get; }
    IEnumerable<string> GetAssemblyPaths(string name, string version);
    IEnumerable<(string, string)> GetDependencies(string name, string version);
}
