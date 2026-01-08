// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// A derived <see cref="SiteContainer"/> that supports adding the @onlyIfNotExists() decorator
/// to the generated Bicep resource statement.
/// </summary>
internal sealed class AspireSiteContainer : SiteContainer
{
    private readonly bool _addOnlyIfNotExistsDecorator;

    /// <summary>
    /// Initializes a new instance of <see cref="AspireSiteContainer"/>.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier name of the resource.</param>
    /// <param name="addOnlyIfNotExistsDecorator">
    /// When <c>true</c>, adds the @onlyIfNotExists() decorator to the resource statement.
    /// </param>
    public AspireSiteContainer(string bicepIdentifier, bool addOnlyIfNotExistsDecorator = false)
        : base(bicepIdentifier)
    {
        _addOnlyIfNotExistsDecorator = addOnlyIfNotExistsDecorator;
    }

    /// <inheritdoc />
    protected override IEnumerable<BicepStatement> Compile()
    {
        foreach (var statement in base.Compile())
        {
            if (_addOnlyIfNotExistsDecorator && statement is ResourceStatement resourceStatement)
            {
                // Add @onlyIfNotExists() decorator to the resource statement
                // Using FunctionCallExpression to generate "onlyIfNotExists()" with parentheses
                resourceStatement.Decorators.Add(
                    new DecoratorExpression(new FunctionCallExpression(new IdentifierExpression("onlyIfNotExists"))));
            }

            yield return statement;
        }
    }
}

/// <summary>
/// A derived <see cref="SiteSlotSiteContainer"/> that supports adding the @onlyIfNotExists() decorator
/// to the generated Bicep resource statement.
/// </summary>
internal sealed class AspireSiteSlotSiteContainer : SiteSlotSiteContainer
{
    private readonly bool _addOnlyIfNotExistsDecorator;

    /// <summary>
    /// Initializes a new instance of <see cref="AspireSiteSlotSiteContainer"/>.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier name of the resource.</param>
    /// <param name="addOnlyIfNotExistsDecorator">
    /// When <c>true</c>, adds the @onlyIfNotExists() decorator to the resource statement.
    /// </param>
    public AspireSiteSlotSiteContainer(string bicepIdentifier, bool addOnlyIfNotExistsDecorator = false)
        : base(bicepIdentifier)
    {
        _addOnlyIfNotExistsDecorator = addOnlyIfNotExistsDecorator;
    }

    /// <inheritdoc />
    protected override IEnumerable<BicepStatement> Compile()
    {
        foreach (var statement in base.Compile())
        {
            if (_addOnlyIfNotExistsDecorator && statement is ResourceStatement resourceStatement)
            {
                // Add @onlyIfNotExists() decorator to the resource statement
                // Using FunctionCallExpression to generate "onlyIfNotExists()" with parentheses
                resourceStatement.Decorators.Add(
                    new DecoratorExpression(new FunctionCallExpression(new IdentifierExpression("onlyIfNotExists"))));
            }

            yield return statement;
        }
    }
}

/// <summary>
/// A derived <see cref="WebSite"/> that supports adding the @onlyIfNotExists() decorator
/// to the generated Bicep resource statement.
/// </summary>
internal sealed class AspireWebSite : WebSite
{
    private readonly bool _addOnlyIfNotExistsDecorator;

    /// <summary>
    /// Initializes a new instance of <see cref="AspireWebSite"/>.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier name of the resource.</param>
    /// <param name="addOnlyIfNotExistsDecorator">
    /// When <c>true</c>, adds the @onlyIfNotExists() decorator to the resource statement.
    /// </param>
    public AspireWebSite(string bicepIdentifier, bool addOnlyIfNotExistsDecorator = false)
        : base(bicepIdentifier)
    {
        _addOnlyIfNotExistsDecorator = addOnlyIfNotExistsDecorator;
    }

    /// <inheritdoc />
    protected override IEnumerable<BicepStatement> Compile()
    {
        foreach (var statement in base.Compile())
        {
            if (_addOnlyIfNotExistsDecorator && statement is ResourceStatement resourceStatement)
            {
                // Add @onlyIfNotExists() decorator to the resource statement
                // Using FunctionCallExpression to generate "onlyIfNotExists()" with parentheses
                resourceStatement.Decorators.Add(
                    new DecoratorExpression(new FunctionCallExpression(new IdentifierExpression("onlyIfNotExists"))));
            }

            yield return statement;
        }
    }
}

/// <summary>
/// A derived <see cref="WebSiteSlot"/> that supports adding the @onlyIfNotExists() decorator
/// to the generated Bicep resource statement.
/// </summary>
internal sealed class AspireWebSiteSlot : WebSiteSlot
{
    private readonly bool _addOnlyIfNotExistsDecorator;

    /// <summary>
    /// Initializes a new instance of <see cref="AspireWebSiteSlot"/>.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier name of the resource.</param>
    /// <param name="addOnlyIfNotExistsDecorator">
    /// When <c>true</c>, adds the @onlyIfNotExists() decorator to the resource statement.
    /// </param>
    public AspireWebSiteSlot(string bicepIdentifier, bool addOnlyIfNotExistsDecorator = false)
        : base(bicepIdentifier)
    {
        _addOnlyIfNotExistsDecorator = addOnlyIfNotExistsDecorator;
    }

    /// <inheritdoc />
    protected override IEnumerable<BicepStatement> Compile()
    {
        foreach (var statement in base.Compile())
        {
            if (_addOnlyIfNotExistsDecorator && statement is ResourceStatement resourceStatement)
            {
                // Add @onlyIfNotExists() decorator to the resource statement
                // Using FunctionCallExpression to generate "onlyIfNotExists()" with parentheses
                resourceStatement.Decorators.Add(
                    new DecoratorExpression(new FunctionCallExpression(new IdentifierExpression("onlyIfNotExists"))));
            }

            yield return statement;
        }
    }
}
