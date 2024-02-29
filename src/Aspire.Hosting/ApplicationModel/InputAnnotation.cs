// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a input annotation that describes an input value.
/// </summary>
/// <remarks>
/// This class is used to specify generated passwords, usernames, etc.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class InputAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputAnnotation"/>.
    /// </summary>
    public InputAnnotation(string name, string? type = null, bool secret = false)
    {
        Name = name;
        Type = type;
        Secret = secret;
    }

    /// <summary>
    /// Name of the input.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The type of the input.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Indicates if the input is a secret.
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    /// Represents what the
    /// </summary>
    public InputDefault? Default { get; set; }
}

/// <summary>
/// Represents how a default value should be retrieved.
/// </summary>
public abstract class InputDefault
{
    /// <summary>
    /// Writes the current <see cref="InputDefault"/> to the manifest context.
    /// </summary>
    /// <param name="context">The context for the manifest publishing operation.</param>
    public abstract void WriteToManifest(ManifestPublishingContext context);
}

/// <summary>
/// Represents that a default value should be generated.
/// </summary>
public sealed class GenerateInputDefault : InputDefault
{
    /// <summary>
    /// The minimum length of the generated value.
    /// </summary>
    public int MinLength { get; set; }

    /// <inheritdoc/>
    public override void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("generate");
        context.Writer.WriteNumber("minLength", MinLength);
        context.Writer.WriteEndObject();
    }
}
