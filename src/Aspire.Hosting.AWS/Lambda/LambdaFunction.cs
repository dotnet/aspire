// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="Resource"/>
/// <seealso cref="ILambdaFunction"/>
internal sealed class LambdaFunction : Resource, ILambdaFunction
{

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    /// <param name="runtime"></param>
    public LambdaFunction(string name, LambdaRuntime runtime) : base(name)
    {
        Runtime = runtime;
    }

    /// <summary>
    ///
    /// </summary>
    public string Handler => this.GetFunctionMetadata().Handler;

    /// <summary>
    ///
    /// </summary>
    public string Path => this.GetFunctionMetadata().ProjectPath;
    /// <summary>
    ///
    /// </summary>
    public LambdaRuntime Runtime { get; }
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    internal async Task WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.lambda.function.v0");
        context.Writer.WriteString("path", context.GetManifestRelativePath(Path));
        context.Writer.WriteString("handler", Handler);
        context.Writer.WriteString("runtime", Runtime.Name);

        await context.WriteEnvironmentVariablesAsync(this).ConfigureAwait(false);
    }
}
