// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.ResourceService.Proto.V1;

namespace Aspire.Dashboard.Model;

public sealed class InteractionsInputsDialogViewModel
{
    public required WatchInteractionsResponseUpdate Interaction { get; init; }
    public required List<InteractionInput> Inputs { get; init; }
}
