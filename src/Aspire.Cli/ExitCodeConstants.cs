// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal static class ExitCodeConstants
{
    public const int Success = 0;
    public const int InvalidCommand = 1;
    public const int FailedToDotnetRunAppHost = 2;
    public const int FailedToInstallTemplates = 3;
    public const int FailedToCreateNewProject = 4;
    public const int FailedToAddPackage = 5;
    public const int FailedToBuildArtifacts = 6;
    public const int FailedToFindProject = 7;
    public const int FailedToTrustCertificates = 8;
    public const int AppHostIncompatible = 9;
}
