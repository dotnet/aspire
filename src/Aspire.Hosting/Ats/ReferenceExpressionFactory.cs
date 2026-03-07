// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Ats;

internal sealed class ReferenceExpressionFactory
{
    private readonly ConstructorInfo _builderConstructor;
    private readonly MethodInfo _appendLiteralMethod;
    private readonly MethodInfo _appendValueProviderMethod;
    private readonly MethodInfo _buildMethod;

    public ReferenceExpressionFactory()
    {
        var builderType = typeof(ReferenceExpressionBuilder);

        _builderConstructor = builderType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException("ReferenceExpressionBuilder does not have a public parameterless constructor.");
        _appendLiteralMethod = builderType.GetMethod(nameof(ReferenceExpressionBuilder.AppendLiteral), [typeof(string)])
            ?? throw new InvalidOperationException("ReferenceExpressionBuilder.AppendLiteral(string) was not found.");
        _appendValueProviderMethod = builderType.GetMethod(nameof(ReferenceExpressionBuilder.AppendValueProvider), [typeof(object), typeof(string)])
            ?? throw new InvalidOperationException("ReferenceExpressionBuilder.AppendValueProvider(object, string?) was not found.");
        _buildMethod = builderType.GetMethod(nameof(ReferenceExpressionBuilder.Build), Type.EmptyTypes)
            ?? throw new InvalidOperationException("ReferenceExpressionBuilder.Build() was not found.");
    }

    public object CreateBuilder() => _builderConstructor.Invoke([]);

    public void AppendLiteral(object builder, string value)
    {
        _appendLiteralMethod.Invoke(builder, [value]);
    }

    public void AppendValueProvider(object builder, object valueProvider)
    {
        _appendValueProviderMethod.Invoke(builder, [valueProvider, null]);
    }

    public object Build(object builder)
    {
        return _buildMethod.Invoke(builder, [])
            ?? throw new InvalidOperationException("ReferenceExpressionBuilder.Build() returned null.");
    }
}
