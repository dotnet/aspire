// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

public class MinioContainerResource(string name, string rootUser, string rootPassword) : ContainerResource(name)
{
    /// <summary>
    /// The Minio root user.
    /// </summary>
    public string RootUser { get; } = rootUser;

    /// <summary>
    /// The Minio root password.
    /// </summary>
    public string RootPassword { get; } = rootPassword;

}
