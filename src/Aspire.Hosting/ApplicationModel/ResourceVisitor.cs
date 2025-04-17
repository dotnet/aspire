// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A base class for visiting resources in an application model.
/// </summary>
[Experimental("ASPRES001")]
public class ResourceVisitor : IResourceVisitor
{
    /// <summary>
    /// Visits the specified <see cref="IResource"/> asynchronously.
    /// </summary>
    /// <param name="resource">The resource to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task VisitAsync(IResource resource)
    {
        await resource.AcceptAsync(this).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Visits the specified value asynchronously.
    /// </summary>
    /// <param name="value">The value to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(object value)
    {
        return value switch
        {
            string s => VisitAsync(s),
            EndpointReference ep => VisitAsync(ep),
            ParameterResource param => VisitAsync(param),
            ConnectionStringReference cs => VisitAsync(cs),
            IResourceWithConnectionString csrs => VisitAsync(csrs),
            EndpointReferenceExpression epExpr => VisitAsync(epExpr),
            ReferenceExpression expr => VisitAsync(expr),
            IValueWithReferences valueWithReferences => valueWithReferences.AcceptAsync(this),
            _ => VisitUnknownAsync(value)
        };
    }

    /// <summary>
    /// Visits an unknown value asynchronously.
    /// </summary>
    /// <param name="value">The unknown value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitUnknownAsync(object value)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="ReferenceExpression"/> asynchronously.
    /// </summary>
    /// <param name="expr">The reference expression to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(ReferenceExpression expr)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="EndpointReferenceExpression"/> asynchronously.
    /// </summary>
    /// <param name="epExpr">The endpoint reference expression to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(EndpointReferenceExpression epExpr)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="IResourceWithConnectionString"/> asynchronously.
    /// </summary>
    /// <param name="csrs">The resource with connection string to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(IResourceWithConnectionString csrs)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="ParameterResource"/> asynchronously.
    /// </summary>
    /// <param name="param">The parameter resource to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(ParameterResource param)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="ConnectionStringReference"/> asynchronously.
    /// </summary>
    /// <param name="cs">The connection string reference to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(ConnectionStringReference cs)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified <see cref="EndpointReference"/> asynchronously.
    /// </summary>
    /// <param name="ep">The endpoint reference to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(EndpointReference ep)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Visits the specified string asynchronously.
    /// </summary>
    /// <param name="s">The string to visit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task VisitAsync(string s)
    {
        return Task.CompletedTask;
    }
}
