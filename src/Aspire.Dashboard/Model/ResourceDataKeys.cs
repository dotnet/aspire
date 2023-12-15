// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public static class ResourceDataKeys
{
    public static class Resource
    {
        public const string Uid = "resource.uid";
        public const string Name = "resource.name";
        public const string Type = "resource.type";
        public const string DisplayName = "resource.displayName";
        public const string State = "resource.state";
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
