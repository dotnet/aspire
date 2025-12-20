// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// Tracks conditional deployment information for resources.
/// This allows resources to be deployed conditionally using the bicep 'if' syntax.
/// </summary>
/// <remarks>
/// This is a temporary solution until Azure.Provisioning SDK natively supports conditional resources.
/// Once the SDK adds native support, this class can be deprecated and removed.
/// </remarks>
internal sealed class ConditionalResourceInfo
{
    /// <summary>
    /// Gets the resource identifier that will be conditionally deployed.
    /// </summary>
    public string ResourceIdentifier { get; }

    /// <summary>
    /// Gets the condition expression string that determines whether the resource should be deployed.
    /// This should be a valid bicep boolean expression (e.g., "firstDeployment" or "not(firstDeployment)").
    /// </summary>
    public string ConditionExpression { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalResourceInfo"/> class.
    /// </summary>
    /// <param name="resourceIdentifier">The bicep identifier of the resource to deploy conditionally.</param>
    /// <param name="conditionExpression">The bicep boolean expression string that determines if the resource should be deployed.</param>
    public ConditionalResourceInfo(string resourceIdentifier, string conditionExpression)
    {
        ResourceIdentifier = resourceIdentifier ?? throw new ArgumentNullException(nameof(resourceIdentifier));
        ConditionExpression = conditionExpression ?? throw new ArgumentNullException(nameof(conditionExpression));
    }
}
