// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Bicep resources to the application model.
/// </summary>
public static class AzureBicepResourceExtensions
{
    /// <summary>
    /// Adds an Azure Bicep resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the deployment name.</param>
    /// <param name="bicepFile">The path to the bicep file on disk. This path is relative to the apphost's project directory.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepResource> AddBicepTemplate(this IDistributedApplicationBuilder builder, string name, string bicepFile)
    {
        var path = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, bicepFile));
        var resource = new AzureBicepResource(name, templateFile: path, templateString: null);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Bicep resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the deployment name.</param>
    /// <param name="bicepContent">A string that represents a snippet of bicep.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepResource> AddBicepTemplateString(this IDistributedApplicationBuilder builder, string name, string bicepContent)
    {
        var resource = new AzureBicepResource(name, templateFile: null, templateString: bicepContent);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Gets a reference to a  output from a bicep template.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">Name of the output.</param>
    /// <returns>A <see cref="BicepOutputReference"/> that represents the output.</returns>
    public static BicepOutputReference GetOutput(this IResourceBuilder<AzureBicepResource> builder, string name)
    {
        return new BicepOutputReference(name, builder.Resource);
    }

    /// <summary>
    /// Gets a reference to a secret output from a bicep template. This is an output that is written to a keyvault using the "keyVaultName" convention.
    /// </summary>
    /// <param name="builder">The resource buider.</param>
    /// <param name="name">The name of the secret output.</param>
    /// <returns>A <see cref="BicepSecretOutputReference"/> that represents the output.</returns>
    public static BicepSecretOutputReference GetSecretOutput(this IResourceBuilder<AzureBicepResource> builder, string name)
    {
        return new BicepSecretOutputReference(name, builder.Resource);
    }

    /// <summary>
    /// Adds an environment variable to the resource with the value of the output from the bicep template.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="bicepOutputReference">The reference to the bicep output.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, BicepOutputReference bicepOutputReference)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(ctx =>
        {
            ctx.EnvironmentVariables[name] = ctx.ExecutionContext.IsPublishMode
                ? bicepOutputReference.ValueExpression
                : bicepOutputReference.Value!;
        });
    }

    /// <summary>
    /// Adds an environment variable to the resource with the value of the secret output from the bicep template.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="bicepOutputReference">The reference to the bicep output.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, BicepSecretOutputReference bicepOutputReference)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(ctx =>
        {
            ctx.EnvironmentVariables[name] = ctx.ExecutionContext.IsPublishMode
                ? bicepOutputReference.ValueExpression
                : bicepOutputReference.Value!;
        });
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/>.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = null;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, string value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, IEnumerable<string> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, JsonNode value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="valueCallback">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, Func<object?> valueCallback)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = valueCallback;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<ParameterResource> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<IResourceWithConnectionString> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    /// <summary>
    /// Adds a parameter to the bicep template.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the input.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithParameter<T>(this IResourceBuilder<T> builder, string name, BicepOutputReference value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }
}
