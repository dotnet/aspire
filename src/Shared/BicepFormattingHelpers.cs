// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Azure.Provisioning.Expressions;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.Utils;

/// <summary>
/// Provides helper methods for formatting Bicep expressions with specific encodings.
/// </summary>
internal sealed class BicepFormattingHelpers
{
    /// <summary>
    /// Formats a Bicep expression using the specified encoding format.
    /// </summary>
    /// <param name="val">The value to format, which must be a <see cref="ProvisioningParameter"/> or <see cref="IBicepValue"/>.</param>
    /// <param name="format">The encoding format to apply. Currently, only "uri" is supported.</param>
    /// <returns>A <see cref="BicepExpression"/> representing the formatted expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the value type is not supported for formatting.</exception>
    /// <exception cref="NotSupportedException">Thrown when the specified format is not supported.</exception>
    public static BicepExpression FormatBicepExpression(object val, string format)
    {
        // Method implementation
    }
}
            ProvisioningParameter p => p.Value.Compile(),
            IBicepValue b => b.Compile(),
            _ => throw new ArgumentException($"Invalid expression type for '{format}' encoding: {val.GetType()}")
        };

        return format.ToLowerInvariant() switch
        {
            "uri" => new FunctionCallExpression(new IdentifierExpression("uriComponent"), innerExpression),
            _ => throw new NotSupportedException($"The format '{format}' is not supported. Supported formats are 'uri' (encodes a URI)")
        };
    }
}
