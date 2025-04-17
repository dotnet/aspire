// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// 
/// </summary>
public interface IResourceVisitor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    Task VisitAsync(object value);
}
