// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Options which control generation of artifacts for deploying to Azure.
/// </summary>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class AzurePublisherOptions : PublishingOptions
{
}
