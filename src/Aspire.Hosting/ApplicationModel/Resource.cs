// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an abstract resource that can be used by an application, that implements <see cref="IResource"/>.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class Resource : IResource
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    public ResourceMetadataCollection Annotations { get; } = new();

    /// <param name="name">The name of the resource.</param>
    public Resource(string name)
    {
        ValidateName(name);

        Name = name;
    }

    /// <summary>
    /// Validate that a resource name is valid.
    /// - Must start with an ASCII letter.
    /// - Must contain only ASCII letters, digits, and hyphens.
    /// - Must not end with a hyphen.
    /// - Must not contain consecutive hyphens.
    /// - Must be between 1 and 64 characters long.
    /// </summary>
    internal static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        if (name.Length > 64)
        {
            throw new ArgumentException($"Resource name '{name}' is invalid. Name must be between 1 and 64 characters long.", nameof(name));
        }

        var lastCharacterHyphen = false;
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '-')
            {
                if (lastCharacterHyphen)
                {
                    throw new ArgumentException($"Resource name '{name}' is invalid. Name cannot contain consecutive hyphens.", nameof(name));
                }
                lastCharacterHyphen = true;
            }
            else if (!char.IsAsciiLetterOrDigit(name[i]))
            {
                throw new ArgumentException($"Resource name '{name}' is invalid. Name must contain only ASCII letters, digits, and hyphens.", nameof(name));
            }
            else
            {
                lastCharacterHyphen = false;
            }
        }

        if (!char.IsAsciiLetter(name[0]))
        {
            throw new ArgumentException($"Resource name '{name}' is invalid. Name must start with an ASCII letter.", nameof(name));
        }

        if (name[^1] == '-')
        {
            throw new ArgumentException($"Resource name '{name}' is invalid. Name cannot end with a hyphen.", nameof(name));
        }
    }

    private string DebuggerToString()
    {
        return $@"Type = {GetType().Name}, Name = ""{Name}""";
    }
}
