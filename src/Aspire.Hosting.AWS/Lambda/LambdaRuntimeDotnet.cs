// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="IEquatable{LambdaRuntimeDotnet}"/>
public sealed class LambdaRuntimeDotnet : IEquatable<LambdaRuntimeDotnet>
{
    /// <summary>
    ///
    /// </summary>
    public static LambdaRuntimeDotnet Default => Dotnet8;
    /// <summary>
    ///
    /// </summary>
    public static LambdaRuntimeDotnet Dotnet6 => new("dotnet6");
    /// <summary>
    ///
    /// </summary>
    public static LambdaRuntimeDotnet Dotnet8 => new("dotnet8");

    /// <summary>
    ///
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    private LambdaRuntimeDotnet(string name)
    {
        Name = name;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(LambdaRuntimeDotnet? other)
    {
        return Name == other?.Name;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is not LambdaRuntimeDotnet runtime)
        {
            return false;
        }

        return runtime.Name == Name;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
