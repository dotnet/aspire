// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
    public string ParameterName => parameterName;
}

#endif
