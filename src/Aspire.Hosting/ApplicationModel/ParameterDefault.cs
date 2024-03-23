// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents how a default value should be retrieved.
/// </summary>
public abstract class ParameterDefault
{
    /// <summary>
    /// Writes the current <see cref="ParameterDefault"/> to the manifest context.
    /// </summary>
    /// <param name="context">The context for the manifest publishing operation.</param>
    public abstract void WriteToManifest(ManifestPublishingContext context);

    /// <summary>
    /// Generates a value for the parameter.
    /// </summary>
    /// <returns>The generated string value.</returns>
    public abstract string GetDefaultValue();
}

/// <summary>
/// Represents that a default value should be generated.
/// </summary>
public sealed class GenerateParameterDefault : ParameterDefault
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
    public override string GetDefaultValue() =>
        PasswordGenerator.Generate(MinLength, Lower, Upper, Numeric, Special, MinLower, MinUpper, MinNumeric, MinSpecial);
}
