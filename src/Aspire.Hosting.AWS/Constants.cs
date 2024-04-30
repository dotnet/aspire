// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS;
internal sealed class Constants
{

    /// <summary>
    /// Error state for Aspire resource dashboard
    /// </summary>
    public const string ResourceStateFailedToStart = "FailedToStart";

    /// <summary>
    /// In progress state for Aspire resource dashboard
    /// </summary>
    public const string ResourceStateStarting = "Starting";

    /// <summary>
    /// Success state for Aspire resource dashboard
    /// </summary>
    public const string ResourceStateRunning = "Running";

    /// <summary>
    /// Environment variable for globally disabling Mock Tools Lambda
    /// </summary>
    public const string MockToolsLambdaDisable = "ASPIRE_AWS_MOCK_TOOLS_LAMBDA_DISABLE";

    /// <summary>
    /// Command Line Argument for AWSLambdaTools in publish mode.
    /// </summary>
    public const string AwsLambdaToolsCreateMissing = "aws-lambda-tools-create-missing";
}
