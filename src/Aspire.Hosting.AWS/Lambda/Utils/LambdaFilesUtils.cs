// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.AWS.Lambda.Utils;

internal static class LambdaFilesUtils
{
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static object CreateServerlessTemplate(IEnumerable<ILambdaFunctionMetadata> functions)
    {
        return new
        {
            AWSTemplateFormatVersion = "2010-09-09",
            Transform = "AWS::Serverless-2016-10-31",
            Description = "Serverless.template",
            Resources = functions.ToDictionary(
                x => x.Handler.Replace("::", "_").Replace(".", "_"),
                x => new { Type = "AWS::Serverless::Function", Properties = new { x.Handler, Runtime = "dotnet8" } })
        };
    }

    public static Dictionary<string, object?> CreateEmptyToolsFile()
    {
        return new Dictionary<string, object?>
        {
            ["information"] = new[]
            {
                "This file provides default values for the deployment wizard inside Visual Studio and the AWS Lambda commands added to the .NET Core CLI.",
                "To learn more about the Lambda commands with the .NET Core CLI execute the following command at the command line in the project root directory.",
                "dotnet lambda help",
                "All the command line options for the Lambda command can be specified in this file."
            },
            ["configuration"] = "Release",
            ["function-memory-size"] = 512,
            ["function-timeout"] = 30,
        };
    }
}
