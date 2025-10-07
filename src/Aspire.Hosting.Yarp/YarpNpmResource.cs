// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// A resource that represents a YARP container configured to host static assets from a Node.js build.
/// </summary>
/// <param name="name">The name of the resource.</param>
[Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public class YarpNpmResource(string name) : YarpResource(name)
{
}
