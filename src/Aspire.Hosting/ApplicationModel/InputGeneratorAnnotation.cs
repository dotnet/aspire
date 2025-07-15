// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal class InputGeneratorAnnotation(Func<ParameterResource, InteractionInput> inputGenerator) : IResourceAnnotation
{
    public Func<ParameterResource, InteractionInput> InputGenerator => inputGenerator;
}
#pragma warning restore ASPIREINTERACTION001
