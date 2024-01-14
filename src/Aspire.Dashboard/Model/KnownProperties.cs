// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Models some known property names for known types of resources.
/// </summary>
/// <remarks>
/// Used as keys in the "properties" dictionary on resource snapshots and view models.
/// Should be compared using <see cref="StringComparers.ResourcePropertyName"/>.
/// </remarks>
public static class KnownProperties
{
    public static class Resource
    {
        public const string Uid = "resource.uid";
        public const string Name = "resource.name";
        public const string Type = "resource.type";
        public const string DisplayName = "resource.displayName";
        public const string State = "resource.state";
        public const string ExitCode = "resource.exitCode";
        public const string CreateTime = "resource.createTime";
    }

    public static class Container
    {
        public const string Id = "container.id";
        public const string Image = "container.image";
        public const string Ports = "container.ports";
        public const string Command = "container.command";
        public const string Args = "container.args";
    }

    public static class Executable
    {
        public const string Path = "executable.path";
        public const string Pid = "executable.pid";
        public const string WorkDir = "executable.workDir";
        public const string Args = "executable.args";
    }

    public static class Project
    {
        public const string Path = "project.path";
    }
}
