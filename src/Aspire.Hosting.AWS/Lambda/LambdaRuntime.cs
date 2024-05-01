// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="IEquatable{LambdaRuntime}"/>
public sealed class LambdaRuntime : IEquatable<LambdaRuntime>
{
    /// <summary>
    ///
    /// </summary>
    public static LambdaRuntime Dotnet6 => new("dotnet6");
    /// <summary>
    ///
    /// </summary>
    public static LambdaRuntime Dotnet8 => new("dotnet8");

    /// <summary>
    ///
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    private LambdaRuntime(string name)
    {
        Name = name;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static LambdaRuntime Custom(string name)
    {
        return new LambdaRuntime(name);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(LambdaRuntime? other)
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
        if (obj is not LambdaRuntime runtime)
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="runtime"></param>
    /// <exception cref="ArgumentOutOfRangeException">null</exception>
    /// <returns></returns>
    internal static LambdaRuntime FromDotnetRuntime(LambdaRuntimeDotnet runtime)
    {
        return runtime.Name switch
        {
            "dotnet6" => Dotnet6,
            "dotnet8" => Dotnet8,
            _ => throw new ArgumentOutOfRangeException(nameof(runtime), runtime, null)
        };
    }
}
