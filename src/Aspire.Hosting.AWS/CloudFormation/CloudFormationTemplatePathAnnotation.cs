// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Annotations that records the template path for a CloudFormation resources.
/// </summary>
/// <param name="templatePath"></param>
[DebuggerDisplay("Type = {GetType().Name,nq}, TemplatePath = {TemplatePath}")]
internal sealed class CloudFormationTemplatePathAnnotation(string templatePath) : IResourceAnnotation
{
    internal string TemplatePath { get; } = templatePath;
}
