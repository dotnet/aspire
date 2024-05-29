// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Resource annotation to change an AWS CDK construct.
/// </summary>
internal interface IConstructModifierAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Changes the AWS CDK construct.
    /// </summary>
    /// <param name="construct">Construct to be changed.</param>
    void ChangeConstruct(IConstruct construct);
}
