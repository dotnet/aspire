// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Wrapper for ArmOperation that exposes wrapped values.
/// </summary>
internal sealed class DefaultArmOperation<T>(ArmOperation<ResourceGroupResource> operation, T wrappedValue) : ArmOperation<T>
{
    public override string Id => operation.Id;
    public override T Value => wrappedValue;
    public override bool HasCompleted => operation.HasCompleted;
    public override bool HasValue => operation.HasValue;
    public override Response GetRawResponse() => operation.GetRawResponse();
    public override Response UpdateStatus(CancellationToken cancellationToken = default) => operation.UpdateStatus(cancellationToken);
    public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) => operation.UpdateStatusAsync(cancellationToken);
    public override ValueTask<Response<T>> WaitForCompletionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override ValueTask<Response<T>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override Response<T> WaitForCompletion(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override Response<T> WaitForCompletion(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}