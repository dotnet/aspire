// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed record ComponentChanged<T>(ObjectChangeType ObjectChangeType, T Component)
    where T : class;
