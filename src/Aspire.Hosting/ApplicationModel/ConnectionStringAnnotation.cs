// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a connection string.
/// </summary>
public class ConnectionStringAnnotation(Func<ReferenceExpression> connectionStringFactory) : IResourceAnnotation
{
    /// <summary>
    /// The connection string.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => connectionStringFactory();
}
