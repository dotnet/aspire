// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Rosetta;

public interface IDependencyContext
{
    string ArtifactsPath { get; }
    IEnumerable<string> GetAssemblyPaths(string name, string version);
    IEnumerable<(string, string)> GetDependencies(string name, string version);
}
