// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// A <see cref="SiteContainer"/> that adds the @onlyIfNotExists() decorator to its bicep output.
/// </summary>
/// <remarks>
/// This decorator ensures the resource is only created if it doesn't already exist,
/// which is useful for deployment scenarios where the parent web app might already exist
/// and only a deployment slot needs to be updated.
/// </remarks>
internal sealed class ConditionalSiteContainer : SiteContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalSiteContainer"/> class.
    /// </summary>
    /// <param name="bicepIdentifier">The bicep identifier for this resource.</param>
    public ConditionalSiteContainer(string bicepIdentifier) : base(bicepIdentifier)
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<BicepStatement> Compile()
    {
        var statements = base.Compile().ToList();

        // Find the ResourceStatement and add the @onlyIfNotExists() decorator
        foreach (var statement in statements)
        {
            if (statement is ResourceStatement resourceStatement)
            {
                // Create the @onlyIfNotExists() decorator
                var decoratorExpression = new FunctionCallExpression(
                    new IdentifierExpression("onlyIfNotExists"));
                
                resourceStatement.Decorators.Add(new DecoratorExpression(decoratorExpression));
            }
        }

        return statements;
    }
}
