// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.Lambda;
using Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
///
/// </summary>
public static class LambdaExtensions
{
    /// <summary>
    /// Adds a Lambda Function to the application model. By default, this will exist in a LambdaFunctions namespace. e.g. LambdaFunctions.MyFunction.
    /// If the Lambda Function is not in a LambdaFunctions namespace, make sure a project reference is added from the AppHost project to the project containing the Lambda Function.
    /// The project also needs to have property `<AWSProjectType>Lambda</AWSProjectType>` set in its project file.
    /// </summary>
    /// <typeparam name="TLambda">A Type that represents the lambda function reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<ILambdaFunction> AddLambdaFunction<TLambda>(this IDistributedApplicationBuilder builder, string name)
        where TLambda : ILambdaFunctionMetadata, new()
    {
        return builder.AddLambdaFunction<TLambda>(name, LambdaRuntimeDotnet.Default, null);
    }

    /// <summary>
    /// Adds a Lambda Function to the application model. By default, this will exist in a LambdaFunctions namespace. e.g. LambdaFunctions.MyFunction.
    /// If the Lambda Function is not in a LambdaFunctions namespace, make sure a project reference is added from the AppHost project to the project containing the Lambda Function.
    /// The project also needs to have property `<AWSProjectType>Lambda</AWSProjectType>` set in its project file.
    /// </summary>
    /// <typeparam name="TLambda">A Type that represents the lambda function reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureMockTool">Configure Lambda Mock Tool</param>
    /// <returns></returns>
    public static IResourceBuilder<ILambdaFunction> AddLambdaFunction<TLambda>(this IDistributedApplicationBuilder builder, string name, Action<MockToolLambdaConfiguration> configureMockTool)
        where TLambda : ILambdaFunctionMetadata, new()
    {
        return builder.AddLambdaFunction<TLambda>(name, LambdaRuntimeDotnet.Default, configureMockTool);
    }

    /// <summary>
    /// Adds a Lambda Function to the application model. By default, this will exist in a LambdaFunctions namespace. e.g. LambdaFunctions.MyFunction.
    /// If the Lambda Function is not in a LambdaFunctions namespace, make sure a project reference is added from the AppHost project to the project containing the Lambda Function.
    /// The project also needs to have property `<AWSProjectType>Lambda</AWSProjectType>` set in its project file.
    /// </summary>
    /// <typeparam name="TLambda">A Type that represents the lambda function reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="runtime">Specify non-default .NET Runtime for this Lambda Function.</param>
    /// <param name="configureMockTool">Configure Lambda Mock Tool</param>
    /// <returns></returns>
    public static IResourceBuilder<ILambdaFunction> AddLambdaFunction<TLambda>(this IDistributedApplicationBuilder builder, string name, LambdaRuntimeDotnet runtime, Action<MockToolLambdaConfiguration>? configureMockTool)
        where TLambda : ILambdaFunctionMetadata, new()
    {
        builder.Services.TryAddLifecycleHook<LambdaFunctionLifecycleHook>();

        var functionMetadata = new TLambda();
        var function = new LambdaFunction(name, LambdaRuntime.FromDotnetRuntime(runtime));

        return builder.AddResource(function)
            .WithAnnotation(functionMetadata)
            .WithManifestPublishingCallback(function.WriteToManifest)
            .ResolveLambdaRuntime(configureMockTool);
    }

    /// <summary>
    /// Add any type of Lambda Function to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="runtime">Runtime for the Function, for example: LambdaRuntime.Custom("nodejs20.x")</param>
    /// <param name="handler">Handler</param>
    /// <param name="relativePath">Relative path from AppHost project to function directory</param>
    /// <returns></returns>
    public static IResourceBuilder<ILambdaFunction> AddLambdaFunction(this IDistributedApplicationBuilder builder, string name, LambdaRuntime runtime, string handler, string relativePath)
    {
        relativePath =
            PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, relativePath));

        var function = new LambdaFunction(name, runtime);
        return builder.AddResource(function)
            .WithAnnotation(new LambdaFunctionMetadata(relativePath, handler))
            .WithManifestPublishingCallback(function.WriteToManifest);
    }

    /// <summary>
    /// Use this to sync Handler and Runtime information to `aws-lambda-tools-defaults.json`.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IDistributedApplicationBuilder AddAWSLambdaToolsSupport(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AWSLambdaToolsPublisher>();
        return builder;
    }

    /// <summary>
    /// Use this to set this Lambda Function as the default function in a project.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IResourceBuilder<ILambdaFunction> SetAsDefaultFunctionInProject(this IResourceBuilder<ILambdaFunction> builder)
    {
        return builder.WithAnnotation(new DefaultFunction());
    }

    /// <summary>
    /// Disable Mock Tool Lambda.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="AWSLambdaException"></exception>
    public static IDistributedApplicationBuilder DisableMockToolLambda(this IDistributedApplicationBuilder builder)
    {
        Environment.SetEnvironmentVariable(Constants.MockToolsLambdaDisable, "true");

        if (builder.Resources.OfType<LambdaFunction>().Any())
        {
            throw new AWSLambdaException("Functions have already been added to the App Model. Make sure to Use 'DisableMockToolLambda' before adding any functions.");
        }

        return builder;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public static ILambdaFunctionMetadata GetFunctionMetadata(this ILambdaFunction function)
    {
        return function.Annotations.OfType<ILambdaFunctionMetadata>().Single();
    }

    internal static bool IsExecutableProject(this ILambdaFunction function)
    {
        return function.GetFunctionMetadata().Traits.Contains("IsExecutableProject");
    }
}
