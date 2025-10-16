// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Azure.Provisioning.Expressions;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.Utils;

internal sealed class BicepFormattingHelpers
{

    public static BicepExpression FormatBicepExpression(object val, string format)
    {
        var innerExpression = val switch
        {
            ProvisioningParameter p => p.Value.Compile(),
            IBicepValue b => b.Compile(),
            _ => throw new ArgumentException($"Invalid expression type for '{format}' encoding: {val.GetType()}")
        };

        return format.ToLowerInvariant() switch
        {
            "uri" => new FunctionCallExpression(new IdentifierExpression("uriComponent"), innerExpression),
            _ => throw new NotSupportedException($"The format '{format}' is not supported. Supported formats are: uri")
        };
    }
}
