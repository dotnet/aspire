// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// A reference to an output parameter of a CloudFormation stack.
/// </summary>
/// <param name="name">The name of the output reference.</param>
/// <param name="resource">The <see cref="ICloudFormationResource"/> resource.</param>
public class StackOutputReference(string name, ICloudFormationResource resource) : IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    /// <summary>
    /// Name of the output.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The instance of the CloudFormation resource.
    /// </summary>
    public ICloudFormationResource Resource { get; } = resource;

    /// <summary>
    /// The value of the output.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        if (Resource.ProvisioningTaskCompletionSource is not null)
        {
            await Resource.ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Value;
    }

    /// <summary>
    /// The value of the output.
    /// </summary>
    public string? Value
    {
        get
        {
            var output = Resource.Outputs?.FirstOrDefault(x => string.Equals(x.OutputKey, Name, StringComparison.Ordinal));
            if (output == null)
            {
                throw new InvalidOperationException($"No output for {Name}");
            }

            return output.OutputValue;
        }
    }

    /// <summary>
    /// The expression used in the manifest to reference the value of the output.
    /// </summary>
    public string ValueExpression => $"{{{Resource.Name}.output.{Name}}}";

    IEnumerable<object> IValueWithReferences.References => [Resource];
}
