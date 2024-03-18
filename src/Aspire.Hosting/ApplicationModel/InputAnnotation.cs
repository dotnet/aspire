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
        ArgumentNullException.ThrowIfNull(name);

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

    /// <summary>
    /// Gets the value of the input.
    /// </summary>
    public string? Value
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

    /// <summary>
    /// Creates a default password input annotation that generates a random password.
    /// </summary>
    /// <param name="password">The hard-coded value of the password to use.</param> // TODO this should be removed with https://github.com/dotnet/aspire/issues/2403
    /// <param name="lower"><see langword="true" /> if lowercase alphabet characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="upper"><see langword="true" /> if uppercase alphabet characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="numeric"><see langword="true" /> if numeric characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="special"><see langword="true" /> if special characters should be included; otherwise, <see langword="false" />.</param>
    /// <param name="minLower">The minimum number of lowercase characters in the result.</param>
    /// <param name="minUpper">The minimum number of uppercase characters in the result.</param>
    /// <param name="minNumeric">The minimum number of numeric characters in the result.</param>
    /// <param name="minSpecial">The minimum number of special characters in the result.</param>
    /// <returns>The created <see cref="InputAnnotation"/> for generating a random password.</returns>
    public static InputAnnotation CreateDefaultPasswordInput(string? password,
        bool lower = true, bool upper = true, bool numeric = true, bool special = true,
        int minLower = 0, int minUpper = 0, int minNumeric = 0, int minSpecial = 0)
    {
        var passwordInput = new InputAnnotation("password", secret: true);
        passwordInput.Default = new GenerateInputDefault
        {
            MinLength = 22, // enough to give 128 bits of entropy when using the default 67 possible characters. See remarks in PasswordGenerator.Generate
            Lower = lower,
            Upper = upper,
            Numeric = numeric,
            Special = special,
            MinLower = minLower,
            MinUpper = minUpper,
            MinNumeric = minNumeric,
            MinSpecial = minSpecial
        };

        if (password is not null)
        {
            passwordInput.SetValueGetter(() => password);
        }

        return passwordInput;
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
    /// Gets or sets the minimum length of the generated value.
    /// </summary>
    public int MinLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include lowercase alphabet characters in the result.
    /// </summary>
    public bool Lower { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include uppercase alphabet characters in the result.
    /// </summary>
    public bool Upper { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include numeric characters in the result.
    /// </summary>
    public bool Numeric { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include special characters in the result.
    /// </summary>
    public bool Special { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of lowercase characters in the result.
    /// </summary>
    public int MinLower { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of uppercase characters in the result.
    /// </summary>
    public int MinUpper { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of numeric characters in the result.
    /// </summary>
    public int MinNumeric { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of special characters in the result.
    /// </summary>
    public int MinSpecial { get; set; }

    /// <inheritdoc/>
    public override void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("generate");
        context.Writer.WriteNumber("minLength", MinLength);

        static void WriteBoolIfNotTrue(ManifestPublishingContext context, string propertyName, bool value)
        {
            if (value != true)
            {
                context.Writer.WriteBoolean(propertyName, value);
            }
        }

        WriteBoolIfNotTrue(context, "lower", Lower);
        WriteBoolIfNotTrue(context, "upper", Upper);
        WriteBoolIfNotTrue(context, "numeric", Numeric);
        WriteBoolIfNotTrue(context, "special", Special);

        static void WriteIntIfNotZero(ManifestPublishingContext context, string propertyName, int value)
        {
            if (value != 0)
            {
                context.Writer.WriteNumber(propertyName, value);
            }
        }

        WriteIntIfNotZero(context, "minLower", MinLower);
        WriteIntIfNotZero(context, "minUpper", MinUpper);
        WriteIntIfNotZero(context, "minNumeric", MinNumeric);
        WriteIntIfNotZero(context, "minSpecial", MinSpecial);

        context.Writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override string GenerateDefaultValue() =>
        PasswordGenerator.Generate(MinLength, Lower, Upper, Numeric, Special, MinLower, MinUpper, MinNumeric, MinSpecial);
}
