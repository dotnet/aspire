// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

// Workaround for ReadOnlySpan<byte> not working as a generic parameter on .NET Framework
public delegate TResult ResponseFunc<TResult>(ReadOnlySpan<byte> data, ILogger logger);
