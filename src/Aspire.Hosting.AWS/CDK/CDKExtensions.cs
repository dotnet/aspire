// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stack = Amazon.CDK.Stack;
using StackResource = Aspire.Hosting.AWS.CDK.StackResource;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding AWS CDK as a provisioning resources.
/// </summary>
public static class CDKExtensions
{
    internal static IDistributedApplicationBuilder AddCDKProvisioning(this IDistributedApplicationBuilder builder)
    {

        builder.AddCloudFormationProvisioning();
        builder.Services.AddSingleton<ICloudFormationProvisionerFactory, CDKProvisionerFactory>();
        return builder;
    }

    /// <summary>
    /// Add a AWS CDK app with stack for provisioning application resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name"></param>
    /// <param name="stackName"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    public static IResourceBuilder<ICDKResource> AddAWSCDK(this IDistributedApplicationBuilder builder, string name, string? stackName = null, IAppProps? props = null)
    {
        builder.AddCDKProvisioning();
        return builder.AddResource(new CDKResource(name, stackName, props));
    }

    /// <summary>
    /// Adds a AWS CDK stack as resource.
    /// </summary>
    /// <param name="builder">The AWS CDK Resource builder.</param>
    /// <param name="name">The name of the stack resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource> AddStack(this IResourceBuilder<ICDKResource> builder, string name)
        => AddStack(builder, name, $"{builder.Resource.StackName}-{name}");

    /// <summary>
    /// Adds a AWS CDK stack as resource.
    /// </summary>
    /// <param name="builder">The AWS CDK Resource builder.</param>
    /// <param name="name">The name of the stack resource.</param>
    /// <param name="stackName">Cloud Formation stack same if different from the resource name.</param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource> AddStack(this IResourceBuilder<ICDKResource> builder, string name, string stackName)
        => builder.AddResource(app => new StackResource(name, new Stack(app.App, stackName), builder.Resource));

    /// <summary>
    /// Adds and build a AWS CDK stack as resource.
    /// </summary>
    /// <param name="builder">The AWS CDK Resource builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="stackBuilder">The stack builder delegate.</param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource<T>> AddStack<T>(this IResourceBuilder<ICDKResource> builder, string name, ConstructBuilderDelegate<T> stackBuilder)
        where T : Stack
        => builder.AddResource(app => new StackResource<T>(name, stackBuilder(app.App), builder.Resource));

    /// <summary>
    /// Adds and build a AWS CDK construct as resource.
    /// </summary>
    /// <param name="builder">The construct resource builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="constructBuilder">The construct builder delegate.</param>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource<T>> AddConstruct<T>(this IResourceBuilder<IResourceWithConstruct> builder, string name, ConstructBuilderDelegate<T> constructBuilder)
        where T : Construct
        => builder.AddResource(parent => new ConstructResource<T>(name, constructBuilder((Construct)parent.Construct), builder.Resource));

    /// <summary>
    /// Adds a stack reference to an output from the CloudFormation stack.
    /// </summary>
    /// <param name="builder">The stack resource builder.</param>
    /// <param name="name">The name of the output.</param>
    /// <param name="output">The construct output delegate.</param>
    /// <typeparam name="TStack"></typeparam>
    /// <example>
    /// The following example shows creating a custom stack and reference the exposed ServiceUrl property
    /// in a project.
    /// <code>
    /// var service = app
    ///     .AddStack("service", scope => new ServiceStack(scope, "ServiceStack")
    ///     .AddOutput("ServiceUrl", stack => stack.Service.ServiceUrl);
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///     .AddReference(service);
    /// </code>
    /// </example>
    public static IResourceBuilder<IStackResource<TStack>> AddOutput<TStack>(
        this IResourceBuilder<IStackResource<TStack>> builder,
        string name, ConstructOutputDelegate<TStack> output)
        where TStack : Stack
    {
        return builder.WithAnnotation(new ConstructOutputAnnotation<TStack>(name, output));
    }

    /// <summary>
    /// Adds a construct reference to an output from the CloudFormation stack.
    /// </summary>
    /// <param name="builder">The construct resource builder.</param>
    /// <param name="name">The name of the output.</param>
    /// <param name="output">The construct output delegate.</param>
    /// <typeparam name="T"></typeparam>
    /// <example>
    /// The following example shows creating a custom construct and reference the exposed ServiceUrl property
    /// in a project.
    /// <code lang="C#">
    /// var service = stack
    ///     .AddConstruct("service", scope => new Service(scope, "service")
    ///     .AddOutput("ServiceUrl", construct => construct.ServiceUrl);
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///     .AddReference(service);
    /// </code>
    /// </example>
    public static IResourceBuilder<IConstructResource<T>> AddOutput<T>(
        this IResourceBuilder<IConstructResource<T>> builder,
        string name, ConstructOutputDelegate<T> output)
        where T : Construct
    {
        return builder.WithAnnotation(new ConstructOutputAnnotation<T>(name, output));
    }

    /// <summary>
    /// Gets a reference to an output from the CloudFormation stack.
    /// </summary>
    /// <param name="builder">The construct resource builder.</param>
    /// <param name="name">The name of the output.</param>
    /// <param name="output">The construct output delegate.</param>
    public static StackOutputReference GetOutput<T>(this IResourceBuilder<IConstructResource<T>> builder, string name, ConstructOutputDelegate<T> output)
        where T : Construct
    {
        builder.WithAnnotation(new ConstructOutputAnnotation<T>(name, output));
        return new StackOutputReference(builder.Resource.Construct.StackUniqueId() + name, builder.Resource.FindParentOfType<StackResource>());
    }

    /// <summary>
    /// Add a reference of a AWS CDK stack to a project. The output parameters of the CloudFormation stack are added to the project IConfiguration.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="stack">The stack resource.</param>
    /// <param name="configSection">The config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IStackResource> stack, string configSection = Constants.DefaultConfigSection)
        where TDestination : IResourceWithEnvironment
    {
        stack.WithAnnotation(new CloudFormationReferenceAnnotation(builder.Resource.Name));

        builder.WithEnvironment(async ctx =>
        {
            // Skip when in publish mode
            if (ctx.ExecutionContext.IsPublishMode)
            {
                return;
            }

            // When the stack has an AWS credentials profile attached, apply that to the resource
            if (stack.Resource.AWSSDKConfig != null)
            {
                SdkUtilities.ApplySDKConfig(ctx, stack.Resource.AWSSDKConfig, false);
            }

            // Wait for the stack to be ready
            if (stack.Resource.ProvisioningTaskCompletionSource is not null)
            {
                ctx.Logger?.LogInformation("Waiting on Stack resource {Name} ...", stack.Resource.Name);
                await stack.Resource.ProvisioningTaskCompletionSource.Task.WaitAsync(ctx.CancellationToken).ConfigureAwait(false);
            }

            // Get the stack outputs and skip when there are none
            var stackOutputs = stack.Resource.Outputs;
            if (stackOutputs == null)
            {
                return;
            }

            configSection = configSection.ToEnvironmentVariables();

            // Add the stack outputs for each child construct to the project configuration
            var processedOutputs = new List<Output>();
            foreach (var construct in stack.Resource.ListChildren(builder.ApplicationBuilder.Resources.OfType<IConstructResource>()))
            {
                var constructId = construct.Construct.StackUniqueId();

                // Filter the outputs attached to the construct
                var outputs = stackOutputs.Where(o => o.OutputKey.StartsWith(constructId)).ToList();

                // Compose the output name with the name of the constructs
                var prefix = configSection.ToEnvironmentVariables();
                var parents = construct.ListParents(builder.ApplicationBuilder.Resources.OfType<IConstructResource>()).ToArray();
                if (parents.Length != 0)
                {
                    prefix += "__" + string.Join("__", parents.Select(r => r.Name));
                }
                prefix += "__" + construct.Name;
                foreach (var output in outputs)
                {
                    var envName = $"{prefix}__{output.OutputKey.TrimStart(constructId)}";
                    ctx.EnvironmentVariables[envName] = output.OutputValue;

                }
                processedOutputs.AddRange(outputs);
            }
            // Add the stack outputs for the stack itself
            foreach (var output in stackOutputs.Except(processedOutputs))
            {
                var envName = $"{configSection}__{output.OutputKey}";
                ctx.EnvironmentVariables[envName] = output.OutputValue;
            }
        });

        return builder;
    }

    /// <summary>
    /// Add an environment variable with a reference of a AWS CDK construct to a project. The output parameters of the CloudFormation stack are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="source">The construct resource.</param>
    /// <param name="outputDelegate">The construct output delegate.</param>
    /// <param name="outputName">The name of the construct output</param>
    /// /// <example>
    /// The following example shows creating a custom construct and reference the exposed ServiceUrl property
    /// in a project as environment variable.
    /// <code lang="C#">
    /// var service = stack.AddConstruct("service", scope => new Service(scope, "service");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///     .WithEnvironment("Service_ServiceUrl", service, s => s.ServiceUrl);
    /// </code>
    /// </example>
    public static IResourceBuilder<TDestination> WithEnvironment<TDestination, TConstruct>(this IResourceBuilder<TDestination> builder, string name, IResourceBuilder<IConstructResource<TConstruct>> source, ConstructOutputDelegate<TConstruct> outputDelegate, string? outputName = default)
        where TConstruct : IConstruct
        where TDestination : IResourceWithEnvironment
    {
        outputName ??= name.Replace("_", string.Empty);
        if (source.Resource.Annotations.OfType<IConstructOutputAnnotation>().All(annotation => annotation.OutputName != outputName))
        {
            source.WithAnnotation(new ConstructOutputAnnotation<TConstruct>(outputName, outputDelegate));
        }
        return builder.WithEnvironment(name, new StackOutputReference(source.Resource.Construct.StackUniqueId() + outputName, source.Resource.FindParentOfType<IStackResource>()));
    }
}
