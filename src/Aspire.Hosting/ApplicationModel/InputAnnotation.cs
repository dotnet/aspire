// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

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
    private string? _value;
    private bool _hasValue;
    private Func<string>? _valueGetter;

    /// <summary>
    /// Initializes a new instance of <see cref="InputAnnotation"/>.
    /// </summary>
    /// <param name="name">The name of the input.</param>
    /// <param name="secret">A flag indicating whether the input is secret.</param>
    public InputAnnotation(string name, bool secret = false)
    {
        Name = name;
        Secret = secret;
    }

    /// <summary>
    /// Name of the input.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Indicates if the input is a secret.
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    /// Represents how the default value of the input should be retrieved.
    /// </summary>
    public InputDefault? Default { get; set; }

    internal string? Value
    {
        get
        {
            if (!_hasValue)
            {
                _value = GenerateValue();
                _hasValue = true;
            }
            return _value;
        }
    }

    internal void SetValueGetter(Func<string> valueGetter) => _valueGetter = valueGetter;

    private string GenerateValue()
    {
        if (_valueGetter is not null)
        {
            return _valueGetter();
        }

        if (Default is null)
        {
            throw new InvalidOperationException("The input does not have a default value.");
        }

        return Default.GenerateDefaultValue();
    }
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

    /// <summary>
    /// Generates a value for the input.
    /// </summary>
    /// <returns>The generated string value.</returns>
    public abstract string GenerateDefaultValue();
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

    /// <inheritdoc/>
    public override string GenerateDefaultValue()
    {
        // https://github.com/Azure/azure-dev/issues/3462 tracks adding more generation options
        return PasswordGenerator.GenerateRandomLettersValue(MinLength);
    }
}
