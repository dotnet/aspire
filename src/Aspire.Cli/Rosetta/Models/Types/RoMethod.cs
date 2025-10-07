// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Rosetta.Models.Types;

[DebuggerDisplay("{ToString(),nq}")]
internal abstract class RoMethod
{
    public abstract RoType DeclaringType { get; }
    public abstract string Name { get; }
    public abstract IReadOnlyList<RoParameterInfo> Parameters { get; }
    public abstract RoType ReturnType { get; }
    public abstract bool IsStatic { get; protected set; }
    public abstract bool IsPublic { get; protected set; }
    public abstract IReadOnlyList<RoType> GetGenericArguments();
    public abstract bool IsGenericMethodDefinition { get; protected set; }
    public abstract bool IsGenericMethod { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the current method is a constructed generic method.
    /// </summary>
    /// <remarks>A constructed generic method is a generic method that has had its type parameters replaced
    /// with specific argument types. This property returns <see langword="true"/> if the method is generic and its type
    /// arguments have been assigned; otherwise, <see langword="false"/>.</remarks>
    // Provided by base (computed)
    public bool IsConstructedGenericMethod => IsGenericMethod && !IsGenericMethodDefinition;

    public abstract int MetadataToken { get; }
    public abstract RoMethod MakeGenericMethod(params RoType[] typeArguments);
    public abstract IEnumerable<RoCustomAttributeData> GetCustomAttributes();

    public override string ToString()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(Name);

        if (GetGenericArguments().Count > 0)
        {
            builder.Append('<');
            builder.Append(string.Join(", ", GetGenericArguments().Select(t => t.ToString())));
            builder.Append('>');
        }
        builder.Append('(');
        builder.Append(string.Join(", ", Parameters.Select(p => p.ParameterType.Name)));
        builder.Append(')');

        builder.Append(" : ");
        builder.Append(ReturnType.Name);

        return builder.ToString();
    }
}
