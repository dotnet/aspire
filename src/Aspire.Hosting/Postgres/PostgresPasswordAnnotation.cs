// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

[DebuggerDisplay("Type = {GetType().Name,nq}, Password = {Password}")]
public class PostgresPasswordAnnotation(string password) : IDistributedApplicationComponentAnnotation
{
    public string Password { get; } = password;
}
