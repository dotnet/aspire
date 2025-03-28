// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresToolsAttribute : Attribute, ITraitAttribute
{
    private readonly string[] _executablesOnPath;

    public RequiresToolsAttribute(string[] executablesOnPath)
    {
        if (executablesOnPath.Length == 0)
        {
            throw new ArgumentException("At least one executable must be provided", nameof(executablesOnPath));
        }

        _executablesOnPath = executablesOnPath;
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (!_executablesOnPath.All(executable => FileUtil.FindFullPathFromPath(executable) is not null))
        {
            return [new KeyValuePair<string, string>(XunitConstants.Category, "failing")];
        }

        return [];
    }
}
