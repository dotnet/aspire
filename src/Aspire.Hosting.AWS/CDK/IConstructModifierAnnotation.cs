// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Resource annotation to change an AWS CDK construct. When a construct is created it doesn't have all the information
/// that it requires. This interface is implemented on resource annotations like <see cref="ConstructOutputAnnotation{T}"/>
/// to add additional outputs referencing construct.
/// </summary>
/// <remarks>
/// This interface is internal and is intended for use by the AWS CDK framework only.
/// </remarks>
internal interface IConstructModifierAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Changes the AWS CDK construct before starting the <see cref="DistributedApplication"/>. This is useful for
    /// altering the construct before it is synthesized, like adding additional outputs.
    /// </summary>
    /// <param name="construct">Construct to be changed.</param>
    void ChangeConstruct(IConstruct construct);
}
