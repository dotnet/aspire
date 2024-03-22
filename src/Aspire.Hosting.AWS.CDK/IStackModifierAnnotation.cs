// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// 
/// </summary>
public interface IStackModifierAnnotation : IResourceAnnotation
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="stack"></param>
    void ChangeStack(Stack stack);
}
