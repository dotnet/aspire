// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Serialization;

namespace Aspire.Hosting.Azure.Extensions;

internal static class EnumExtensions
{
    /// <summary>
    /// Reads the value of an enum out of the attached <see cref="EnumMemberAttribute"/> attribute.
    /// </summary>
    /// <typeparam name="T">The enum.</typeparam>
    /// <param name="value">The value of the enum to pull the value for.</param>
    /// <returns></returns>
    public static string GetValueFromEnumMember<T>(this T value) where T : Enum
    {
        var memberInfo = typeof(T).GetMember(value.ToString(),
            BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        if (memberInfo.Length <= 0)
        {
            return value.ToString();
        }

        var attributes = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
        if (attributes.Length > 0)
        {
            var targetAttribute = (EnumMemberAttribute) attributes[0];
            if (targetAttribute is {Value: not null})
            {
                return targetAttribute.Value;
            }
        }

        return value.ToString();
    }

    /// <summary>
    /// Casts an enum provided as a flag to a list of discrete enum values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The enum property to create the list from.</param>
    /// <returns></returns>
    public static List<T> ToList<T>(this T source) where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Where(enumValue => source.HasFlag(enumValue))
            .ToList();
    }
}
