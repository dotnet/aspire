// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// This interface is used to mark a construct resource so that it can be identified as an output
/// during synthesis. This is needed in to map the output to the environment variables after synthesizing
/// that would not normally be possible.
/// </summary>
/// <remarks>
/// This interface is internal and is intended for use by the AWS CDK framework only.
/// </remarks>
internal interface IConstructOutputAnnotation
{
    string OutputName { get; }
}
