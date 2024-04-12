// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IConstructModifierAnnotation : IResourceAnnotation
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="construct"></param>
    void ChangeConstruct(IConstruct construct);
}
